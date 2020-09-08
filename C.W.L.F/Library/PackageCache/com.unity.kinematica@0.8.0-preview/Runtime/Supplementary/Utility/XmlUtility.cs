using System.Xml;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Kinematica
{
    internal static class XmlUtility
    {
        public static void CreateAttribute(this XmlNode node, string name, string value)
        {
            var document = node.OwnerDocument;
            var attribute = document.CreateAttribute(name);
            attribute.Value = value;
            node.Attributes.Append(attribute);
        }

        public static void CreateAttribute(this XmlNode node, string name, int value)
        {
            node.CreateAttribute(name, value.ToString());
        }

        public static void CreateAttribute(this XmlNode node, string name, float value)
        {
            node.CreateAttribute(name, value.ToString());
        }

        public static void CreateVector3Node(this XmlNode parentNode, string name, float3 position)
        {
            var document = parentNode.OwnerDocument;
            var node = document.CreateElement(name);
            node.CreateAttribute("x", position.x);
            node.CreateAttribute("y", position.y);
            node.CreateAttribute("z", position.z);
            parentNode.AppendChild(node);
        }

        public static void CreateQuaternionNode(this XmlNode parentNode, string name, quaternion rotation)
        {
            var document = parentNode.OwnerDocument;
            var node = document.CreateElement(name);
            node.CreateAttribute("x", rotation.value.x);
            node.CreateAttribute("y", rotation.value.y);
            node.CreateAttribute("z", rotation.value.z);
            node.CreateAttribute("w", rotation.value.w);
            parentNode.AppendChild(node);
        }
    }
}
