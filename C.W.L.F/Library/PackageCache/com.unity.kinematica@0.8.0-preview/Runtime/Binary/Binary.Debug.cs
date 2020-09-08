using System;
using System.Text;
using System.Xml;
using Unity.Mathematics;

namespace Unity.Kinematica
{
    public partial struct Binary
    {
        public string GetFragmentDebugText(SamplingTime samplingTime, string fragmentName, float speedTimeOffset = -1.0f)
        {
            AnimationSampleTimeIndex animSampleTime = GetAnimationSampleTimeIndex(samplingTime.timeIndex);
            if (!animSampleTime.IsValid)
            {
                return $"<b>{fragmentName}:</b> Invalid fragment";
            }

            string speedText = speedTimeOffset >= 0.0f ? $", <b>Speed:</b> {GetAverageTrajectorySpeed(samplingTime, speedTimeOffset):0.000} m/s" : "";

            return $"<b>{fragmentName}:</b> {animSampleTime.clipName}, <b>Frame:</b> {animSampleTime.animFrameIndex}{speedText}";
        }

        public float GetAverageTrajectorySpeed(SamplingTime samplingTime, float timeOffset)
        {
            float deltaTime = math.rcp(SampleRate);

            float trajectoryTimeSpan = deltaTime * 2.0f;

            return math.length(GetTrajectoryVelocity(samplingTime, timeHorizon * timeOffset, trajectoryTimeSpan));
        }

        internal XmlDocument GenerateDebugDocument()
        {
            XmlDocument document = new XmlDocument();

            XmlNode node = document.CreateElement("Binary");
            document.AppendChild(node);

            GenerateRig(node);

            GenerateTypes(node);
            GenerateTraits(node);

            GenerateSegments(node);
            GenerateTags(node);
            GenerateMarkers(node);
            GenerateIntervals(node);
            GenerateTagLists(node);
            GenerateMetrics(node);
            GenerateCodeBooks(node);

            return document;
        }

        void GenerateRig(XmlNode parentNode)
        {
            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("AnimationRig");
            parentNode.AppendChild(rootNode);

            ref var rig = ref animationRig;

            for (int i = 0; i < rig.NumJoints; ++i)
            {
                var nameIndex = rig.bindPose[i].nameIndex;

                var jointNode = document.CreateElement("Joint");
                jointNode.CreateAttribute("name", GetString(nameIndex));
                jointNode.CreateAttribute("parent", rig.bindPose[i].parentIndex);
                jointNode.CreateVector3Node("Position", rig.bindPose[i].localTransform.t);
                jointNode.CreateQuaternionNode("Rotation", rig.bindPose[i].localTransform.q);
                rootNode.AppendChild(jointNode);
            }
        }

        void GenerateSegments(XmlNode parentNode)
        {
            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("Segments");
            parentNode.AppendChild(rootNode);

            for (int i = 0; i < numSegments; ++i)
            {
                ref var segment = ref GetSegment(i);

                var node = document.CreateElement("Segment");
                node.CreateAttribute("index", i);
                node.CreateAttribute("name", GetString(segment.nameIndex));
                node.CreateAttribute("tagIndex", segment.tagIndex);
                node.CreateAttribute("numTags", segment.numTags);
                node.CreateAttribute("markerIndex", segment.markerIndex);
                node.CreateAttribute("numMarkers", segment.numMarkers);
                node.CreateAttribute("intervalIndex", segment.intervalIndex);
                node.CreateAttribute("numIntervals", segment.numIntervals);
                rootNode.AppendChild(node);

                var sourceNode = document.CreateElement("Source");
                sourceNode.CreateAttribute("firstFrame", segment.source.FirstFrame);
                sourceNode.CreateAttribute("numFrames", segment.source.NumFrames);
                node.AppendChild(sourceNode);

                var destinationNode = document.CreateElement("Destination");
                destinationNode.CreateAttribute("firstFrame", segment.destination.FirstFrame);
                destinationNode.CreateAttribute("numFrames", segment.destination.NumFrames);
                node.AppendChild(destinationNode);
            }
        }

        void GenerateTypes(XmlNode parentNode)
        {
            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("Types");
            parentNode.AppendChild(rootNode);

            for (int i = 0; i < numTypes; ++i)
            {
                var type = GetType(i);

                var typeNode = document.CreateElement("Type");
                typeNode.CreateAttribute("name", GetTypeName(type));
                typeNode.CreateAttribute("size", type.numBytes);
                rootNode.AppendChild(typeNode);

                if (type.numFields > 0)
                {
                    for (int j = 0; j < type.numFields; ++j)
                    {
                        int fieldIndex = type.fieldIndex + j;
                        var fieldNode = document.CreateElement("Field");
                        var fieldType = GetType(GetField(fieldIndex).typeIndex);
                        fieldNode.CreateAttribute("name", GetTypeName(fieldType));
                        typeNode.AppendChild(fieldNode);
                    }
                }
            }
        }

        void GenerateTraits(XmlNode parentNode)
        {
            string ByteArrayToString(byte[] byteArray)
            {
                var stringBuilder = new StringBuilder();
                for (int i = 0; i < byteArray.Length; i++)
                {
                    stringBuilder.Append(byteArray[i].ToString("x2"));
                }
                return stringBuilder.ToString();
            }

            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("Traits");
            parentNode.AppendChild(rootNode);

            for (int i = 0; i < numTraits; ++i)
            {
                var trait = GetTrait(i);
                var type = GetType(trait.typeIndex);

                var node = document.CreateElement("Trait");
                node.CreateAttribute("index", i);
                node.CreateAttribute("type", GetTypeName(type));

                var payload = GetTraitPayload(i);
                node.CreateAttribute("payload", ByteArrayToString(payload));

                rootNode.AppendChild(node);
            }
        }

        void GenerateTags(XmlNode parentNode)
        {
            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("Tags");
            parentNode.AppendChild(rootNode);

            for (int i = 0; i < numTags; ++i)
            {
                var tag = GetTag(i);

                var node = document.CreateElement("Tag");

                //var trait = GetTrait(tag.traitIndex);
                //var type = GetType(trait.typeIndex);
                //node.CreateAttribute("type", GetTypeName(type));

                node.CreateAttribute("index", i);
                node.CreateAttribute("trait", tag.traitIndex);
                node.CreateAttribute("segment", tag.segmentIndex);
                node.CreateAttribute("firstFrame", tag.firstFrame);
                node.CreateAttribute("numFrames", tag.numFrames);

                rootNode.AppendChild(node);
            }
        }

        void GenerateMarkers(XmlNode parentNode)
        {
            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("Markers");
            parentNode.AppendChild(rootNode);

            for (int i = 0; i < numMarkers; ++i)
            {
                var marker = GetMarker(i);

                var node = document.CreateElement("Marker");

                node.CreateAttribute("index", i);
                node.CreateAttribute("trait", marker.traitIndex);
                node.CreateAttribute("frameIndex", marker.frameIndex);

                rootNode.AppendChild(node);
            }
        }

        void GenerateIntervals(XmlNode parentNode)
        {
            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("Intervals");
            parentNode.AppendChild(rootNode);

            for (int i = 0; i < numIntervals; ++i)
            {
                var interval = GetInterval(i);

                var node = document.CreateElement("Interval");

                node.CreateAttribute("index", i);
                node.CreateAttribute("segment", interval.segmentIndex);
                node.CreateAttribute("firstFrame", interval.firstFrame);
                node.CreateAttribute("numFrames", interval.numFrames);
                node.CreateAttribute("tagList", interval.tagListIndex);
                node.CreateAttribute("codeBook", interval.codeBookIndex);

                rootNode.AppendChild(node);
            }
        }

        void GenerateTagLists(XmlNode parentNode)
        {
            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("TagLists");
            parentNode.AppendChild(rootNode);

            for (int i = 0; i < numTagLists; ++i)
            {
                var tagList = GetTagList(i);

                var node = document.CreateElement("TagList");
                node.CreateAttribute("index", i);

                var stringBuilder = new StringBuilder();
                for (int j = 0; j < tagList.numIndices; ++j)
                {
                    var tagIndex =
                        tagIndices[tagList.tagIndicesIndex + j];
                    var tagIndexString =
                        tagIndex.value.ToString();
                    stringBuilder.Append(tagIndexString + ';');
                }
                node.CreateAttribute(
                    "indices", stringBuilder.ToString());

                rootNode.AppendChild(node);
            }
        }

        void GenerateMetrics(XmlNode parentNode)
        {
            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("Metrics");
            parentNode.AppendChild(rootNode);

            for (int i = 0; i < numMetrics; ++i)
            {
                ref var metric = ref GetMetric(i);

                var node = document.CreateElement("Metric");

                node.CreateAttribute("index", i);
                node.CreateAttribute("poseTimeSpan", metric.poseTimeSpan);

                for (int j = 0; j < metric.joints.Length; ++j)
                {
                    var jointName = GetString(metric.joints[j].nameIndex);
                    var jointIndex = metric.joints[j].jointIndex;

                    var jointNode = document.CreateElement("Joint");
                    jointNode.CreateAttribute("name", jointName);
                    jointNode.CreateAttribute("index", jointIndex);
                    node.AppendChild(jointNode);
                }

                rootNode.AppendChild(node);
            }
        }

        void GenerateCodeBooks(XmlNode parentNode)
        {
            var document = parentNode.OwnerDocument;
            var rootNode = document.CreateElement("CodeBooks");
            parentNode.AppendChild(rootNode);

            for (int i = 0; i < numCodeBooks; ++i)
            {
                var codeBook = GetCodeBook(i);

                var node = document.CreateElement("CodeBook");

                node.CreateAttribute("index", i);
                node.CreateAttribute("metric", codeBook.metricIndex);
                node.CreateAttribute("trait", codeBook.traitIndex);

                rootNode.AppendChild(node);
            }
        }
    }
}
