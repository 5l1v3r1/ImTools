﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ImTools.Experimental
{
    public class ImHashTree<K, V>
    {
        public static readonly ImHashTree<K, V> Empty = new EmptyNode();

        public int Hash { get { return _payload.Hash; } }

        public K Key { get { return _payload.Key; } }
        public V Value { get { return _payload.Value; } }

        public KeyValuePair<K, V>[] Conflicts
        {
            get
            {
                var p = _payload as PayloadWithConflicts;
                return p == null ? null : p.Conflicts;
            }
        }

        public virtual bool IsEmpty { get { return false; } }
        public virtual int Height { get { return 1; } }

        public virtual ImHashTree<K, V> Left { get { return Empty; } }
        public virtual ImHashTree<K, V> Right { get { return Empty; } }

        /// <summary>Returns true if key is found and sets the value.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">Result value</param>
        /// <returns>True if key found, false otherwise.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public bool TryFind(K key, out V value)
        {
            var hash = key.GetHashCode();
            var t = this;

            var p = t._payload;
            if (!t.IsEmpty && hash != p.Hash)
                t = hash < p.Hash ? t.Left : t.Right;

            p = t._payload;
            if (!t.IsEmpty && hash != p.Hash)
                t = hash < p.Hash ? t.Left : t.Right;

            p = t._payload;
            if (!t.IsEmpty && hash != p.Hash)
                t = hash < p.Hash ? t.Left : t.Right;

            p = t._payload;
            while (t.Height != 0 && hash != p.Hash)
            {
                t = hash < p.Hash ? t.Left : t.Right;
                p = t._payload;
            }

            if (t.Height != 0 && (ReferenceEquals(key, p.Key) || key.Equals(p.Key)))
            {
                value = p.Value;
                return true;
            }

            return t.TryFindConflictedValue(key, out value);
        }

        /// <summary>Returns true if key is found and sets the value.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">Result value</param>
        /// <returns>True if key found, false otherwise.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public bool TryFind2(K key, out V value)
        {
            var t = this;
            var hash = key.GetHashCode();
            while (t.Height != 0 && t.Hash != hash)
                t = hash < t.Hash ? t.Left : t.Right;

            if (t.Height != 0 && (ReferenceEquals(key, t.Key) || key.Equals(t.Key)))
            {
                value = t.Value;
                return true;
            }

            return t.TryFindConflictedValue(key, out value);
        }

        /// <summary>Looks for key in a tree and returns the key value if found, or <paramref name="defaultValue"/> otherwise.</summary>
        /// <param name="key">Key to look for.</param> <param name="defaultValue">(optional) Value to return if key is not found.</param>
        /// <returns>Found value or <paramref name="defaultValue"/>.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public V GetValueOrDefault(K key, V defaultValue = default(V))
        {
            var t = this;
            var hash = key.GetHashCode();
            while (t.Height != 0 && t.Hash != hash)
                t = hash < t.Hash ? t.Left : t.Right;
            return t.Height != 0 && (ReferenceEquals(key, t.Key) || key.Equals(t.Key))
                ? t.Value : t.GetConflictedValueOrDefault(key, defaultValue);
        }

        /// <summary>Returns new tree with added key-value. 
        /// If value with the same key is exist then the value is replaced.</summary>
        /// <param name="key">Key to add.</param><param name="value">Value to add.</param>
        /// <returns>New tree with added or updated key-value.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImHashTree<K, V> AddOrUpdate(K key, V value)
        {
            return AddOrUpdate(key.GetHashCode(), key, value);
        }

        /// <summary>Depth-first in-order traversal as described in http://en.wikipedia.org/wiki/Tree_traversal
        /// The only difference is using fixed size array instead of stack for speed-up (~20% faster than stack).</summary>
        /// <returns>Sequence of enumerated key value pairs.</returns>
        public IEnumerable<KeyValuePair<K, V>> Enumerate()
        {
            if (IsEmpty)
                yield break;

            var parents = new ImHashTree<K, V>[Height];

            var node = this;
            var parentCount = -1;
            while (node.Height != 0 || parentCount != -1)
            {
                if (node.Height != 0)
                {
                    parents[++parentCount] = node;
                    node = node.Left;
                }
                else
                {
                    node = parents[parentCount--];
                    yield return new KeyValuePair<K, V>(node.Key, node.Value);

                    var payloadWithConflicts = node._payload as PayloadWithConflicts;
                    if (payloadWithConflicts != null)
                    {
                        var conflicts = payloadWithConflicts.Conflicts;
                        for (var i = 0; i < conflicts.Length; i++)
                            yield return conflicts[i];
                    }

                    node = node.Right;
                }
            }
        }

        /// <summary>Removes or updates value for specified key, or does nothing if key is not found.
        /// Based on Eric Lippert http://blogs.msdn.com/b/ericlippert/archive/2008/01/21/immutability-in-c-part-nine-academic-plus-my-avl-tree-implementation.aspx </summary>
        /// <param name="key">Key to look for.</param> 
        /// <returns>New tree with removed or updated value.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImHashTree<K, V> Remove(K key)
        {
            return Remove(key.GetHashCode(), key);
        }

        #region Implementation

        protected virtual ImHashTree<K, V> With(Payload payload)
        {
            return new ImHashTree<K, V>(payload);
        }

        private bool TryFindConflictedValue(K key, out V value)
        {
            if (!IsEmpty)
            {
                var payloadWithConflicts = _payload as PayloadWithConflicts;
                if (payloadWithConflicts != null)
                {
                    var conflicts = payloadWithConflicts.Conflicts;
                    for (var i = conflicts.Length - 1; i >= 0; --i)
                        if (Equals(conflicts[i].Key, key))
                        {
                            value = conflicts[i].Value;
                            return true;
                        }
                }
            }

            value = default(V);
            return false;
        }

        private V GetConflictedValueOrDefault(K key, V defaultValue)
        {
            var payloadWithConflicts = _payload as PayloadWithConflicts;
            if (payloadWithConflicts != null)
            {
                var conflicts = payloadWithConflicts.Conflicts;
                for (var i = conflicts.Length - 1; i >= 0; --i)
                    if (Equals(conflicts[i].Key, key))
                        return conflicts[i].Value;
            }
            return defaultValue;
        }

        private ImHashTree<K, V> AddOrUpdate(int hash, K key, V value)
        {
            return Height == 0  // add new node
                ? LeafNode(hash, key, value)
                : (hash == Hash // update found node
                    ? (ReferenceEquals(Key, key) || Key.Equals(key)
                        ? UpdatePayload(hash, key, value)
                        : UpdateConflicts(key, value))
                : (hash < Hash  // search for node
                    ? (Height == 1
                        ? new BranchNode(_payload, LeafNode(hash, key, value), Right, height: 2)
                        : new BranchNode(_payload, Left.AddOrUpdate(hash, key, value), Right).KeepBalance())
                    : (Height == 1
                        ? new BranchNode(_payload, Left, LeafNode(hash, key, value), height: 2)
                        : new BranchNode(_payload, Left, Right.AddOrUpdate(hash, key, value)).KeepBalance())));
        }

        private ImHashTree<K, V> UpdatePayload(int hash, K key, V value)
        {
            var payloadWithConflicts = _payload as PayloadWithConflicts;
            return With(payloadWithConflicts == null
                ? new Payload(hash, key, value)
                : new PayloadWithConflicts(payloadWithConflicts.Conflicts, hash, key, value));
        }

        private ImHashTree<K, V> UpdateConflicts(K key, V value)
        {
            var p = _payload;
            var payloadWithConflicts = p as PayloadWithConflicts;
            if (payloadWithConflicts == null)
                return With(new PayloadWithConflicts(new[] { new KeyValuePair<K, V>(key, value) }, p.Hash, p.Key, p.Value));

            var conflicts = payloadWithConflicts.Conflicts;
            var found = conflicts.Length - 1;
            while (found >= 0 && !Equals(conflicts[found].Key, Key)) --found;
            if (found == -1)
            {
                var addedConflicts = new KeyValuePair<K, V>[conflicts.Length + 1];
                Array.Copy(conflicts, 0, addedConflicts, 0, conflicts.Length);
                addedConflicts[conflicts.Length] = new KeyValuePair<K, V>(key, value);

                return With(new PayloadWithConflicts(addedConflicts, p.Hash, p.Key, p.Value));
            }

            var updatedConflicts = new KeyValuePair<K, V>[conflicts.Length];
            Array.Copy(conflicts, 0, updatedConflicts, 0, conflicts.Length);
            updatedConflicts[found] = new KeyValuePair<K, V>(key, value);

            return With(new PayloadWithConflicts(updatedConflicts, p.Hash, p.Key, p.Value));
        }

        private ImHashTree<K, V> Remove(int hash, K key, bool ignoreKey = false)
        {
            if (IsEmpty)
                return this;

            ImHashTree<K, V> result;
            if (hash == Hash) // found node
            {
                if (ignoreKey || Equals(Key, key))
                {
                    if (!ignoreKey)
                    {
                        var payloadWithConflicts = _payload as PayloadWithConflicts;
                        if (payloadWithConflicts != null)
                            return LiftFirstConflict(payloadWithConflicts);
                    }

                    if (Height == 1) // remove node
                        return Empty;

                    if (Right.IsEmpty)
                        result = Left;
                    else if (Left.IsEmpty)
                        result = Right;
                    else
                    {
                        // we have two children, so remove the next highest node and replace this node with it.
                        var next = Right;
                        while (!next.Left.IsEmpty)
                            next = next.Left;
                        result = next.With(Left, Right.Remove(next.Hash, default(K), ignoreKey: true));
                    }
                }
                else if (_payload is PayloadWithConflicts)
                    return TryRemoveConflicted((PayloadWithConflicts)_payload, key);
                else
                    return this; // if key is not matching and no conflicts to lookup - just return
            }
            else if (hash < Hash)
                result = With(Left.Remove(hash, key, ignoreKey), Right);
            else
                result = With(Left, Right.Remove(hash, key, ignoreKey));

            return result.KeepBalance();
        }

        private ImHashTree<K, V> LiftFirstConflict(PayloadWithConflicts payload)
        {
            var conflicts = payload.Conflicts;
            var firstConflict = conflicts[0];
            if (conflicts.Length == 1)
                return With(new Payload(payload.Hash, firstConflict.Key, firstConflict.Value));

            var remainingConflicts = new KeyValuePair<K, V>[conflicts.Length - 1];
            Array.Copy(conflicts, 1, remainingConflicts, 0, remainingConflicts.Length);

            return With(new PayloadWithConflicts(remainingConflicts, payload.Hash, firstConflict.Key, firstConflict.Value));
        }

        private ImHashTree<K, V> TryRemoveConflicted(PayloadWithConflicts payload, K keyToRemove)
        {
            var conflicts = payload.Conflicts;
            var index = conflicts.Length - 1;
            while (index >= 0 && !Equals(conflicts[index].Key, keyToRemove))
                --index;
            if (index == -1) // key is not found in conflicts - just return current node
                return this;

            if (conflicts.Length == 1) // remove conflicts
                return With(new Payload(payload.Hash, payload.Key, payload.Value));

            var shrinkedConflicts = new KeyValuePair<K, V>[conflicts.Length - 1];
            var newIndex = 0;
            for (var i = 0; i < conflicts.Length; ++i)
                if (i != index)
                    shrinkedConflicts[newIndex++] = conflicts[i];

            return With(new PayloadWithConflicts(shrinkedConflicts, payload.Hash, payload.Key, payload.Value));
        }

        private ImHashTree<K, V> KeepBalance()
        {
            var delta = Left.Height - Right.Height;
            if (delta >= 2) // left is longer by 2, rotate left
            {
                var left = Left;
                var leftLeft = left.Left;
                var leftRight = left.Right;
                if (leftRight.Height - leftLeft.Height == 1)
                {
                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2     6      4     6      2     5
                    // 1   4        2   3        1   3     6
                    //    3        1
                    return leftRight.With(
                        left.With(leftLeft, leftRight.Left),
                        With(leftRight.Right, Right));
                }

                // todo: do we need this?
                // one rotation:
                //      5     =>     2
                //   2     6      1     5
                // 1   4              4   6
                return left.With(leftLeft, With(leftRight, Right));
            }

            if (delta <= -2)
            {
                var right = Right;
                var rightLeft = right.Left;
                var rightRight = right.Right;
                if (rightLeft.Height - rightRight.Height == 1)
                {
                    return rightLeft.With(
                        With(Left, rightLeft.Left),
                        right.With(rightLeft.Right, rightRight));
                }

                return right.With(With(Left, rightLeft), rightRight);
            }

            return this;
        }

        [MethodImpl(MethodImplHints.AggressingInlining)]
        private ImHashTree<K, V> With(ImHashTree<K, V> left, ImHashTree<K, V> right)
        {
            return left.IsEmpty && right.IsEmpty
                ? new ImHashTree<K, V>(_payload)
                : new BranchNode(_payload, left, right);
        }

        protected class Payload
        {
            public readonly K Key;
            public readonly V Value;

            public readonly int Hash;

            public Payload(int hash, K key, V value)
            {
                Key = key;
                Value = value;
                Hash = hash;
            }
        }

        private sealed class PayloadWithConflicts : Payload
        {
            public readonly KeyValuePair<K, V>[] Conflicts;

            public PayloadWithConflicts(KeyValuePair<K, V>[] conflicts,
                int hash, K key, V value) : base(hash, key, value)
            {
                Conflicts = conflicts;
            }
        }

        private readonly Payload _payload;

        protected ImHashTree(Payload payload)
        {
            _payload = payload;
        }

        private static ImHashTree<K, V> LeafNode(int hash, K key, V value)
        {
            return new ImHashTree<K, V>(new Payload(hash, key, value));
        }

        private sealed class EmptyNode : ImHashTree<K, V>
        {
            public EmptyNode() : base(null) { }

            public override bool IsEmpty { get { return true; } }
            public override int Height { get { return 0; } }
        }

        private sealed class BranchNode : ImHashTree<K, V>
        {
            public override int Height { get { return _height; } }
            public override ImHashTree<K, V> Left { get { return _left; } }
            public override ImHashTree<K, V> Right { get { return _right; } }

            protected override ImHashTree<K, V> With(Payload payload)
            {
                return new BranchNode(payload, _left, _right);
            }

            private readonly ImHashTree<K, V> _left, _right;
            private readonly int _height;

            public BranchNode(Payload payload, ImHashTree<K, V> left, ImHashTree<K, V> right)
                : base(payload)
            {
                _left = left;
                _right = right;
                _height = 1 + (left.Height > right.Height ? left.Height : right.Height);
            }

            public BranchNode(Payload payload, ImHashTree<K, V> left, ImHashTree<K, V> right, int height)
                : base(payload)
            {
                _left = left;
                _right = right;
                _height = height;
            }
        }

        #endregion
    }
}