using DAWGCompressor;
using System.Collections.Immutable;

const string FILE_EXTENSION = "dawgx";
const string DEFAULT_OUTPUT_FILENAME = $"dictionary.{FILE_EXTENSION}";

static LetterGroup WordListToLetterGroup(string[] words)
{
    LetterGroup startGroup = new();
    // aggregate profiling counters (low-overhead)
    long totalComparisons = 0;
    long totalJoinAttempts = 0;
    long totalJoinSuccess = 0;
    long totalJoinFailed = 0;
    long totalParentUpdates = 0;
    var joinTimer = new System.Diagnostics.Stopwatch();
    Dictionary<LetterGroup, Dictionary<char, HashSet<LetterGroup>>> parents = new() { { startGroup, [] } };
    Dictionary<char, HashSet<LetterGroup>> leafParents = [];
    Dictionary<LetterGroup, HashSet<char>> zipperGroups = [];
    HashSet<char> zipperLeaves = [];

    // first pass: create the trie structure
    foreach (string word in words)
    {
        LetterGroup currentGroup = startGroup;

        for (int i = 0; i < word.Length; i++)
        {
            char letter = word[i];
            bool isFinalLetter = i == word.Length - 1;

            if (isFinalLetter)
            {
                currentGroup.Leaves.Add(letter);

                if (!leafParents.TryGetValue(letter, out HashSet<LetterGroup>? groupList))
                {
                    leafParents[letter] = groupList = [];
                    zipperLeaves.Add(letter);
                }
                groupList.Add(currentGroup);
            }
            else
            {
                if (!currentGroup.InternalLetters.TryGetValue(letter, out LetterGroup? nextGroup))
                {
                    currentGroup.InternalLetters.Add(letter, nextGroup = new());
                    parents[nextGroup] = new()
                    {
                        {letter, new([currentGroup]) }
                    };
                }
                currentGroup = nextGroup;
            }
        }
    }

    Dictionary<LetterGroup, HashSet<LetterGroup>> nonMatchingGroups = [];

    // second pass: compress the trie structure into a DAWG
    while (zipperLeaves.Count > 0 || zipperGroups.Count > 0)
    {
        HashSet<LetterGroup> setToJoin;

        if (zipperLeaves.Count > 0)
        {
            char leaf = zipperLeaves.First();
            zipperLeaves.Remove(leaf);

            setToJoin = leafParents[leaf];
        }
        else
        {
            KeyValuePair<LetterGroup, HashSet<char>> groupEntry = zipperGroups.First();
            char node = groupEntry.Value.First();

            groupEntry.Value.Remove(node);
            if (groupEntry.Value.Count == 0)
            {
                zipperGroups.Remove(groupEntry.Key);
            }

            setToJoin = parents[groupEntry.Key][node];
        }

        JoinGroupSet(setToJoin);
    }

    Console.WriteLine($"DAWG Profile: comparisons={totalComparisons}, joinAttempts={totalJoinAttempts}, joinSuccess={totalJoinSuccess}, joinFailed={totalJoinFailed}, parentUpdates={totalParentUpdates}, joinTimeMs={joinTimer.ElapsedMilliseconds}");
    return startGroup;

    void JoinGroupSet(HashSet<LetterGroup> groupSet)
    {
        // compute chain lengths once to avoid repeated recursion in GetChainLength
        var ordered = groupSet.Select(g => (Group: g, Len: GetChainLength(g.Head)))
            .OrderBy(x => x.Len)
            .Select(x => x.Group);
        LinkedList<LetterGroup> setAsList = new(ordered);
        LinkedListNode<LetterGroup>? node1 = setAsList.First!;

        while (node1 != null)
        {
            //nonMatchingGroups.TryGetValue(node1.Value.Head, out HashSet<LetterGroup>? nonMatchingPool);

            LinkedListNode<LetterGroup>? node2 = node1.Next;
            while (node2 != null)
            {
                //if(!(nonMatchingPool?.Contains(node2.Value.Head) ?? false))
                //{
                LetterGroup? joinedNode = JoinGroups(node1.Value, node2.Value);

                if (joinedNode == null)
                {
                    /*if(nonMatchingPool == null)
                    {
                        nonMatchingGroups[node1.Value.Head] = nonMatchingPool = [];
                    }
                    nonMatchingPool.Add(node2.Value.Head);*/
                }
                else
                {
                    node1.Value = joinedNode;
                    node2 = node2.Previous;
                    setAsList.Remove(node2!.Next!);
                }
                //}

                node2 = node2.Next;
            }

            node1 = node1.Next;
        }
    }

    LetterGroup? JoinGroups(LetterGroup group1, LetterGroup group2)
    {
        totalJoinAttempts++;
        joinTimer.Start();
        group1 = group1.Tail;
        group2 = group2.Tail;

        if (group1 == group2)
        {
            return group1;
        }

        Queue<(LetterGroup, (LetterGroup?, LetterGroup?))>? newChain = null;
        Dictionary<LetterGroup, LetterGroup>? replacements = null;
        LetterGroup? iterator1 = group1;
        LetterGroup? iterator2 = group2;
        LetterGroup? bigGroup1 = group1;
        LetterGroup? bigGroup2 = group2;
        while (iterator1 != null || iterator2 != null)
        {
            if (iterator1 != null && iterator2 != null)
            {
                totalComparisons++;
                GroupComparison? comparison = GetGroupComparison(iterator1, iterator2);

                if (comparison == null)
                {
                    totalJoinFailed++;
                    joinTimer.Stop();
                    return null;
                }

                replacements ??= [];
                newChain ??= [];

                comparison.Deconstruct(out LetterGroup intersection, out LetterGroup difference, out bool reversed);

                LetterGroup? difference1;
                LetterGroup? difference2;
                if (reversed)
                {
                    difference2 = difference;
                    difference1 = null;
                }
                else
                {
                    difference1 = difference;
                    difference2 = null;
                }

                int diff1Count = GetGroupLength(difference1);
                int diff2Count = GetGroupLength(difference2);

                if (diff1Count > 0)
                {
                    if (diff2Count > 0)
                    {
                        return null;
                    }

                    newChain.Enqueue((intersection, (null, bigGroup2)));
                    replacements[bigGroup2!] = intersection;

                    iterator1 = difference1;
                    iterator2 = bigGroup2!.PreviousEntry;
                    bigGroup2 = iterator2;
                }
                else
                {
                    if (diff2Count > 0)
                    {
                        newChain.Enqueue((intersection, (bigGroup1, null)));
                        replacements[bigGroup1!] = intersection;

                        iterator1 = bigGroup1!.PreviousEntry;
                        bigGroup1 = iterator1;
                        iterator2 = difference2;
                    }
                    else
                    {
                        newChain.Enqueue((intersection, (bigGroup1, bigGroup2)));
                        replacements[bigGroup1!] = intersection;
                        replacements[bigGroup2!] = intersection;

                        iterator1 = bigGroup1!.PreviousEntry;
                        bigGroup1 = iterator1;
                        iterator2 = bigGroup2!.PreviousEntry;
                        bigGroup2 = iterator2;
                    }
                }
            }
            else
            {
                LetterGroup remainingClone = GetCloneWithReplacedTargets(iterator1 ?? iterator2!);

                newChain!.Enqueue((remainingClone, (bigGroup1, bigGroup2)));
                if (bigGroup1 != null)
                {
                    replacements![bigGroup1] = remainingClone;
                    iterator1 = bigGroup1!.PreviousEntry;
                    bigGroup1 = iterator1;
                }
                if (bigGroup2 != null)
                {
                    replacements![bigGroup2] = remainingClone;
                    iterator2 = bigGroup2!.PreviousEntry;
                    bigGroup2 = iterator2;
                }
            }
        }

        LetterGroup? result = null;
        while (newChain!.Count > 0)
        {
            (LetterGroup, (LetterGroup?, LetterGroup?)) chainEntry = newChain.Dequeue();

            if (result == null)
            {
                result = chainEntry.Item1;
            }
            else
            {
                result.PreviousEntry = chainEntry.Item1;
                result = result.Head;
            }

            // set the new groups as parents of their children
            foreach (KeyValuePair<char, LetterGroup> childEntry in chainEntry.Item1.InternalLetters)
            {
                parents[childEntry.Value][childEntry.Key].Add(chainEntry.Item1);
            }
            foreach (char leaf in chainEntry.Item1.Leaves)
            {
                leafParents[leaf].Add(chainEntry.Item1);
            }

            // determine the parent and zippergroup sets for each of the new groups
            if (chainEntry.Item2.Item1 == null)
            {
                if (chainEntry.Item2.Item2 != null)
                {
                    parents[chainEntry.Item1] = parents[chainEntry.Item2.Item2];

                    if (zipperGroups.TryGetValue(chainEntry.Item2.Item2, out HashSet<char>? zipperChars))
                    {
                        zipperGroups[chainEntry.Item1] = zipperChars;
                        zipperGroups.Remove(chainEntry.Item2.Item2);
                    }
                }
            }
            else
            {
                if (chainEntry.Item2.Item2 == null)
                {
                    parents[chainEntry.Item1] = parents[chainEntry.Item2.Item1];

                    if (zipperGroups.TryGetValue(chainEntry.Item2.Item1, out HashSet<char>? zipperChars))
                    {
                        zipperGroups[chainEntry.Item1] = zipperChars;
                        zipperGroups.Remove(chainEntry.Item2.Item1);
                    }
                }
                else
                {
                    parents[chainEntry.Item1] = parents[chainEntry.Item2.Item1].Concat(parents[chainEntry.Item2.Item2])
                        .GroupBy(entry => entry.Key)
                        .ToDictionary(entry => entry.Key, entry => entry.SelectMany(e => e.Value).ToHashSet());

                    HashSet<char>? computedZipperGroup;

                    HashSet<char> commonChars = [.. parents[chainEntry.Item2.Item1].Keys.Where(parents[chainEntry.Item2.Item2].ContainsKey)];
                    if (commonChars.Count > 0)
                    {
                        computedZipperGroup = commonChars;
                        zipperGroups[chainEntry.Item1] = computedZipperGroup;
                    }
                    else
                    {
                        computedZipperGroup = null;
                    }

                    if (zipperGroups.TryGetValue(chainEntry.Item2.Item1, out HashSet<char>? zipperChars))
                    {
                        if (computedZipperGroup == null)
                        {
                            computedZipperGroup = zipperChars;
                            zipperGroups[chainEntry.Item1] = computedZipperGroup;
                        }
                        else
                        {
                            computedZipperGroup.UnionWith(zipperChars);
                        }
                        zipperGroups.Remove(chainEntry.Item2.Item1);
                    }

                    if (zipperGroups.TryGetValue(chainEntry.Item2.Item2, out zipperChars))
                    {
                        if (computedZipperGroup == null)
                        {
                            computedZipperGroup = zipperChars;
                            zipperGroups[chainEntry.Item1] = computedZipperGroup;
                        }
                        else
                        {
                            computedZipperGroup.UnionWith(zipperChars);
                        }
                        zipperGroups.Remove(chainEntry.Item2.Item2);
                    }

                }
            }

            // remove the old groups from their sons parent lists
            foreach (LetterGroup? oldGroup in new LetterGroup?[] { chainEntry.Item2.Item1, chainEntry.Item2.Item2 })
            {
                if (oldGroup != null)
                {
                    foreach (KeyValuePair<char, LetterGroup> childEntry in oldGroup.InternalLetters)
                    {
                        if (parents.TryGetValue(childEntry.Value, out Dictionary<char, HashSet<LetterGroup>>? childParents))
                        {
                            childParents[childEntry.Key].Remove(oldGroup);
                        }
                    }
                    foreach (char leaf in oldGroup.Leaves)
                    {
                        leafParents[leaf].Remove(oldGroup);
                    }
                }
            }

            // update the parents to point to the new groups
            foreach (KeyValuePair<char, HashSet<LetterGroup>> parentsEntry in parents[chainEntry.Item1])
            {
                foreach (LetterGroup parent in parentsEntry.Value)
                {
                    parent.InternalLetters[parentsEntry.Key] = chainEntry.Item1;
                }
            }

            // count parent updates for profiling
            if (parents.TryGetValue(chainEntry.Item1, out var pmap))
            {
                foreach (var kv in pmap)
                {
                    totalParentUpdates += kv.Value.Count;
                }
            }
            // remove the old groups from the parents dictionary
            if (chainEntry.Item2.Item1 != null)
            {
                parents.Remove(chainEntry.Item2.Item1);
            }
            if (chainEntry.Item2.Item2 != null)
            {
                parents.Remove(chainEntry.Item2.Item2);
            }

        }

        // there is a start group, update it in case it was removed
        if (group1.Head == startGroup)
        {
            startGroup = result!;
        }
        else
        {
            if (group2.Head == startGroup)
            {
                startGroup = result!;
            }
        }

        return result;

        LetterGroup GetCloneWithReplacedTargets(LetterGroup originalGroup)
        {
            return new(new(originalGroup.InternalLetters.ToDictionary(entry => entry.Key, entry => GetTrueTarget(entry.Value))), [.. originalGroup.Leaves]);
        }

        LetterGroup GetTrueTarget(LetterGroup originalTarget)
        {
            return replacements?.GetValueOrDefault(originalTarget, originalTarget) ?? originalTarget;
        }

        GroupComparison? GetGroupComparison(LetterGroup group1, LetterGroup group2)
        {
            if (group1 == group2)
            {
                return null;
            }

            LetterGroup swallower;
            LetterGroup swallowed;
            bool reversed;

            int leavesDifference = group1.Leaves.Count - group2.Leaves.Count;
            int internalDifference = group1.InternalLetters.Count - group2.InternalLetters.Count;

            if (internalDifference == 0)
            {
                if (leavesDifference < 0)
                {
                    swallower = group2;
                    swallowed = group1;
                    leavesDifference = -leavesDifference;
                    reversed = true;
                }
                else
                {
                    swallower = group1;
                    swallowed = group2;
                    reversed = false;
                }
            }
            else
            {
                if (internalDifference < 0)
                {
                    swallower = group2;
                    swallowed = group1;
                    internalDifference = -internalDifference;
                    leavesDifference = -leavesDifference;
                    reversed = true;
                }
                else
                {
                    swallower = group1;
                    swallowed = group2;
                    reversed = false;
                }

                if (leavesDifference < 0)
                {
                    return null;
                }
            }

            List<char>? intersectionLeaves = null;
            List<char>? differenceLeaves = null;
            List<KeyValuePair<char, LetterGroup>>? intersectionInternal = null;
            List<KeyValuePair<char, LetterGroup>>? differenceInternal = null;

            foreach (char leaf in swallower.Leaves)
            {
                if (swallowed.Leaves.Contains(leaf))
                {
                    (intersectionLeaves ??= new()).Add(leaf);
                }
                else
                {
                    if (--leavesDifference < 0)
                    {
                        return null;
                    }
                    (differenceLeaves ??= new()).Add(leaf);
                }
            }

            foreach (KeyValuePair<char, LetterGroup> internalEntry in swallower.InternalLetters)
            {
                if (swallowed.InternalLetters.TryGetValue(internalEntry.Key, out LetterGroup? target))
                {
                    LetterGroup trueTarget = GetTrueTarget(internalEntry.Value);
                    if (trueTarget == GetTrueTarget(target))
                    {
                        (intersectionInternal ??= new()).Add(new KeyValuePair<char, LetterGroup>(internalEntry.Key, trueTarget));
                    }
                    else
                    {
                        if (--internalDifference < 0)
                        {
                            return null;
                        }
                        (differenceInternal ??= new()).Add(internalEntry);
                    }
                }
                else
                {
                    if (--internalDifference < 0)
                    {
                        return null;
                    }
                    (differenceInternal ??= new()).Add(internalEntry);
                }
            }

            return new(
                new((intersectionInternal ?? new List<KeyValuePair<char, LetterGroup>>()).ToDictionary(entry => entry.Key, entry => entry.Value), [.. (intersectionLeaves ?? new List<char>())]),
                new((differenceInternal ?? new List<KeyValuePair<char, LetterGroup>>()).ToDictionary(entry => entry.Key, entry => entry.Value), [.. (differenceLeaves ?? new List<char>())]),
                reversed
                );
        }
    }

    static int GetGroupLength(LetterGroup? group)
    {
        return group == null ? 0 : group.InternalLetters.Count + group.Leaves.Count;
    }

    static int GetChainLength(LetterGroup? group)
    {
        return group == null ? 0 : (GetGroupLength(group) + GetChainLength(group.NextEntry));
    }
}

/// <summary>
/// 7 bits: letter count (L), J = bit count of L
/// 16 bits: first letter (F)
/// 4 bits: bit count of alphabet span (B)
/// L* B bits: letter - F
/// 5 bits: node index bit count (N)
/// K* (J+N+1) bits: letter index, node index (1-based), is last sibling
/// </summary>
static byte[] SerializeLetterGroup(LetterGroup letterGroup)
{
    HashSet<char> alphabet = [];

    LinkedList<Node> nodeList = [];
    LinkedListNode<Node>? nodeListNode = null;
    Dictionary<LetterGroup, int> letterGroupIndices = [];
    LetterGroup? childrenToAdd = letterGroup.Head;
    HashSet<LetterGroup> letterGroupsBin = [childrenToAdd];

    do
    {
        do
        {
            letterGroupIndices[childrenToAdd!] = nodeList.Count;

            Dictionary<char, LetterGroup>.Enumerator childrenEnumerator = childrenToAdd!.InternalLetters.GetEnumerator();
            if (childrenEnumerator.MoveNext())
            {
                bool isLastInternalNode;
                do
                {
                    KeyValuePair<char, LetterGroup> current = childrenEnumerator.Current;
                    isLastInternalNode = !childrenEnumerator.MoveNext();
                    nodeList.AddLast(new Node(current.Key, current.Value, childrenToAdd.NextEntry == null && childrenToAdd.Leaves.Count == 0 && isLastInternalNode));
                    alphabet.Add(current.Key);
                }
                while (!isLastInternalNode);
            }

            HashSet<char>.Enumerator leavesEnumerator = childrenToAdd.Leaves.GetEnumerator();
            if (leavesEnumerator.MoveNext())
            {
                bool isLastLeaf;
                do
                {
                    char current = leavesEnumerator.Current;
                    isLastLeaf = !leavesEnumerator.MoveNext();
                    nodeList.AddLast(new Node(current, null, childrenToAdd.NextEntry == null && isLastLeaf));
                    alphabet.Add(current);
                }
                while (!isLastLeaf);
            }

            childrenToAdd = childrenToAdd.NextEntry;
        }
        while (childrenToAdd != null);

        do
        {
            nodeListNode = nodeListNode == null ? nodeList.First : nodeListNode.Next;
        }
        while (nodeListNode != null && (nodeListNode.Value.Children == null || !letterGroupsBin.Add(nodeListNode.Value.Children.Head)));

        if (nodeListNode != null)
        {
            childrenToAdd = nodeListNode.Value.Children!.Head;
        }

    } while (nodeListNode != null);

    int nodeCount = nodeList.Count;
    int letterCount = alphabet.Count;
    char firstLetter = alphabet.Min();
    int alphabetSpan = alphabet.Max() - firstLetter + 1;
    byte alphabetSpanBitCount = BitCount(alphabetSpan);
    byte letterIndexBitCount = BitCount(letterCount);
    byte nodeIndexBitCount = BitCount(nodeCount + 1);

    ByteArrayBuilder nodesBytesBuilder = new(32 + alphabetSpanBitCount * letterCount + nodeCount * (letterIndexBitCount + nodeIndexBitCount + 1));

    nodesBytesBuilder.AppendBits(letterCount, 7);
    nodesBytesBuilder.AppendBits(firstLetter, 16);
    nodesBytesBuilder.AppendBits(alphabetSpanBitCount, 4);

    List<char> listedAlphabet = [.. alphabet];
    foreach (char letter in listedAlphabet)
    {
        nodesBytesBuilder.AppendBits(letter - firstLetter, alphabetSpanBitCount);
    }

    nodesBytesBuilder.AppendBits(nodeIndexBitCount, 5);

    int currentIndex = 1;
    int maxForward = 0;
    int maxBackward = 0;
    foreach (Node nodeEntry in nodeList)
    {
        int absoluteIndex = nodeEntry.Children == null ? 0 : letterGroupIndices[nodeEntry.Children] + 1;

        nodesBytesBuilder.AppendBits(nodeEntry.Letter - firstLetter, letterIndexBitCount);
        nodesBytesBuilder.AppendBits(absoluteIndex, nodeIndexBitCount);
        nodesBytesBuilder.AppendBits(nodeEntry.IsLastSibling ? 1 : 0, 1);

        if (absoluteIndex != 0)
        {
            if (absoluteIndex > currentIndex)
            {
                maxForward = Math.Max(maxForward, absoluteIndex - currentIndex);
            }
            else
            {
                if (absoluteIndex < currentIndex)
                {
                    maxBackward = Math.Max(maxBackward, currentIndex - absoluteIndex);
                }
            }
        }

        currentIndex++;
    }

    Console.WriteLine($"Between {-maxBackward} and {maxForward}. Current bit count for indices is {nodeIndexBitCount}.");
    Console.WriteLine($"Chains: {letterGroupsBin.Count}.");

    return nodesBytesBuilder.ToByteArray();

    static byte BitCount(int value)
    {
        return (byte)Math.Ceiling(Math.Log2(value));
    }
}

if (args.Length < 1)
{
    Console.WriteLine($"Usage: DAWGCompressor.exe [input file] [output file name (def. {DEFAULT_OUTPUT_FILENAME})]. Ex: DAWGCompressor.exe engwords.txt engdic.{FILE_EXTENSION}");
    return 1;
}

string inputFilename = args[0];
string outputFilename = args.Length > 1 ? args[1] : DEFAULT_OUTPUT_FILENAME;

string[] words = File.ReadAllLines(inputFilename);

LetterGroup wordStructure = WordListToLetterGroup(words);
File.WriteAllBytes(outputFilename, SerializeLetterGroup(wordStructure));

return 0;

record GroupComparison(LetterGroup Intersection, LetterGroup Difference, bool Reversed);git init -b main
git add .
git status
git commit -m "Initial commit"