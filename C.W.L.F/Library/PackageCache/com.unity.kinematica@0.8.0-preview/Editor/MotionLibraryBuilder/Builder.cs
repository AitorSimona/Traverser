using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder : IDisposable
    {
        public event Action<ProgressInfo> progressFeedback;

        public Builder(Asset asset)
        {
            allocator = new BlobAllocator(-1);

            ref var binary = ref allocator.ConstructRoot<Binary>();

            this.binary = MemoryRef<Binary>.Create(ref binary);

            this.asset = asset;

            stringTable = StringTable.Create();

            rig = AnimationRig.Create(asset.DestinationAvatar);
        }

        public void Dispose()
        {
            allocator.Dispose();
        }

        public IEnumerator BuildAsync(string filePath)
        {
            binary.Ref.FileVersion = Binary.s_CodeVersion;
            binary.Ref.SampleRate = asset.SampleRate;
            binary.Ref.TimeHorizon = asset.TimeHorizon;

            IEnumerator state = null;

            state = LoadAnimationClips();
            while (state.MoveNext())
            {
                yield return null;
                if (bCancel)
                {
                    yield break;
                }
            }

            BuildAnimationRig();
            BuildSegments();

            state = BuildTransforms();
            while (state.MoveNext())
            {
                yield return null;
                if (bCancel)
                {
                    state.MoveNext(); // give opportunity to enumerator to release resources
                    yield break;
                }
            }

            BuildTags();
            BuildMetrics();

            state = BuildFragments();
            while (state.MoveNext())
            {
                yield return null;
                if (bCancel)
                {
                    state.MoveNext(); // give opportunity to enumerator to release resources
                    yield break;
                }
            }

            BuildStringTable();

            VerifyIntegrity();

            BlobFile.WriteBlobAsset(allocator, ref Binary, filePath);

            Binary.GenerateDebugDocument().Save(
                Path.ChangeExtension(filePath, ".debug.xml"));
        }

        public bool Cancelled => bCancel;

        public void Cancel()
        {
            bCancel = true;
        }

        public static Builder Create(Asset asset)
        {
            return new Builder(asset);
        }

        public ref Binary Binary
        {
            get { return ref binary.Ref; }
        }

        void VerifyIntegrity()
        {
            ref Binary binary = ref Binary;

            int numIntervals = binary.numIntervals;
            int numSegments = binary.numSegments;
            int numCodeBooks = binary.numCodeBooks;

            // Verify reference integrity between segments, tags and intervals.

            var expectedTagIndex = 0;
            var expectedIntervalIndex = 0;

            for (int i = 0; i < numSegments; ++i)
            {
                var segment = binary.GetSegment(i);

                Assert.IsTrue(segment.tagIndex == expectedTagIndex);
                Assert.IsTrue(segment.intervalIndex == expectedIntervalIndex);

                for (int j = 0; j < segment.numTags; ++j)
                {
                    Assert.IsTrue(binary.GetTag(
                        segment.tagIndex + j).segmentIndex == i);
                }

                for (int j = 0; j < segment.numIntervals; ++j)
                {
                    Assert.IsTrue(binary.GetInterval(
                        segment.intervalIndex + j).segmentIndex == i);
                }

                expectedTagIndex += segment.numTags;
                expectedIntervalIndex += segment.numIntervals;
            }

            Assert.IsTrue(expectedTagIndex == binary.numTags);
            Assert.IsTrue(expectedIntervalIndex == binary.numIntervals);

            // Verify reference integrity between intervals and codebooks.

            for (int i = 0; i < numCodeBooks; ++i)
            {
                int numCodeBookIntervals =
                    binary.codeBooks[i].intervals.Length;

                for (int j = 0; j < numCodeBookIntervals; ++j)
                {
                    int intervalIndex =
                        binary.codeBooks[i].intervals[j];

                    Assert.IsTrue(
                        binary.intervals[intervalIndex].codeBookIndex == i);
                }
            }

            for (int i = 0; i < numIntervals; ++i)
            {
                var codeBookIndex =
                    binary.intervals[i].codeBookIndex;

                if (codeBookIndex != Binary.CodeBookIndex.Invalid)
                {
                    Assert.IsTrue(
                        binary.codeBooks[codeBookIndex].Contains(i));
                }
            }
        }

        IEnumerator LoadAnimationClips()
        {
            IEnumerable<TaggedAnimationClip> taggedClips = asset.AnimationLibrary.Where(c => !c.IsClipLoaded);
            int clipCount = taggedClips.Count();
            int clipIndex = 0;

            foreach (TaggedAnimationClip taggedClip in taggedClips)
            {
                ++clipIndex;

                progressFeedback?.Invoke(new ProgressInfo()
                {
                    title = $"Loading clip {taggedClip.ClipName}",
                    progress = clipIndex / (float)clipCount
                });

                taggedClip.GetOrLoadClipSync(false);
                yield return null;
            }
        }

        //
        // Asset
        //

        Asset asset;

        //
        // Intermediate representation
        //

        List<ClipSegments> clipSegments;

        int numFrames;

        int numSegments;

        StringTable stringTable;

        AnimationRig rig;

        //
        // Final memory-ready representation
        //

        MemoryRef<Binary> binary;

        BlobAllocator allocator;

        //
        // Misc
        //

        bool bCancel = false;
    }
}
