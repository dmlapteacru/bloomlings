using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Bloomlings.ArtGen
{
    /// <summary>A triangle mesh read from a model file: positions, and a normal per triangle corner.</summary>
    public sealed class TriangleMesh
    {
        public TriangleMesh(V3[] positions, int[] triangles, V3[] cornerNormals)
        {
            Positions = positions;
            Triangles = triangles;
            CornerNormals = cornerNormals;
        }

        /// <summary>Vertex positions.</summary>
        public V3[] Positions { get; }

        /// <summary>Three vertex indices per triangle.</summary>
        public int[] Triangles { get; }

        /// <summary>One normal per triangle corner (index i of <see cref="Triangles"/>).</summary>
        public V3[] CornerNormals { get; }
    }

    /// <summary>
    /// A minimal reader for binary FBX 7.x files with one polygon mesh (the Leafling experiment, research R17): it reads
    /// the mesh's vertices, polygons (fanned into triangles) and normals, and nothing else (no materials, textures,
    /// skins or animation). Coordinates stay as stored.
    /// </summary>
    public static class Fbx
    {
        private sealed class Node
        {
            public string Name = string.Empty;
            public List<object> Properties = new List<object>();
            public List<Node> Children = new List<Node>();

            public Node? Child(string name) => Children.Find(c => c.Name == name);
        }

        public static TriangleMesh ReadMesh(string file)
        {
            byte[] data = File.ReadAllBytes(file);
            if (data.Length < 27 || Encoding.ASCII.GetString(data, 0, 18) != "Kaydara FBX Binary")
            {
                throw new InvalidDataException(file + " is not a binary FBX file.");
            }

            int version = BitConverter.ToInt32(data, 23);
            bool wide = version >= 7500;
            var roots = new List<Node>();
            int offset = 27;
            while (offset < data.Length)
            {
                Node? node = ReadNode(data, ref offset, wide);
                if (node == null)
                {
                    break;
                }

                roots.Add(node);
            }

            Node objects = roots.Find(n => n.Name == "Objects") ?? throw new InvalidDataException("No Objects in " + file);
            Node geometry = objects.Children.Find(n => n.Name == "Geometry") ?? throw new InvalidDataException("No mesh in " + file);
            double[] vertices = (double[])geometry.Child("Vertices")!.Properties[0];
            int[] polygons = (int[])geometry.Child("PolygonVertexIndex")!.Properties[0];
            Node normalLayer = geometry.Child("LayerElementNormal") ?? throw new InvalidDataException("No normals in " + file);
            double[] normals = (double[])normalLayer.Child("Normals")!.Properties[0];
            int[]? normalIndex = normalLayer.Child("NormalsIndex")?.Properties[0] as int[];
            string mapping = (string)normalLayer.Child("MappingInformationType")!.Properties[0];
            bool byCorner = mapping == "ByPolygonVertex";

            var positions = new V3[vertices.Length / 3];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = new V3(vertices[i * 3], vertices[(i * 3) + 1], vertices[(i * 3) + 2]);
            }

            V3 Normal(int corner, int vertex)
            {
                int n = byCorner ? corner : vertex;
                if (normalIndex != null)
                {
                    n = normalIndex[n];
                }

                return new V3(normals[n * 3], normals[(n * 3) + 1], normals[(n * 3) + 2]);
            }

            var triangles = new List<int>();
            var cornerNormals = new List<V3>();
            int start = 0;
            for (int i = 0; i < polygons.Length; i++)
            {
                if (polygons[i] >= 0)
                {
                    continue;
                }

                // A negative index (bitwise not) ends a polygon; fan it into triangles.
                int Vertex(int k) => k == i ? ~polygons[k] : polygons[k];
                for (int k = start + 1; k < i; k++)
                {
                    foreach (int corner in new[] { start, k, k + 1 })
                    {
                        triangles.Add(Vertex(corner));
                        cornerNormals.Add(Normal(corner, Vertex(corner)));
                    }
                }

                start = i + 1;
            }

            return new TriangleMesh(positions, triangles.ToArray(), cornerNormals.ToArray());
        }

        private static Node? ReadNode(byte[] data, ref int offset, bool wide)
        {
            long end = wide ? BitConverter.ToInt64(data, offset) : BitConverter.ToUInt32(data, offset);
            long count = wide ? BitConverter.ToInt64(data, offset + 8) : BitConverter.ToUInt32(data, offset + 4);
            int header = wide ? 24 : 12;
            int nameLength = data[offset + header];
            if (end == 0)
            {
                offset += header + 1;
                return null;
            }

            var node = new Node { Name = Encoding.ASCII.GetString(data, offset + header + 1, nameLength) };
            int p = offset + header + 1 + nameLength;
            for (long i = 0; i < count; i++)
            {
                node.Properties.Add(ReadProperty(data, ref p));
            }

            while (p < end)
            {
                Node? child = ReadNode(data, ref p, wide);
                if (child == null)
                {
                    break;
                }

                node.Children.Add(child);
            }

            offset = (int)end;
            return node;
        }

        private static object ReadProperty(byte[] data, ref int p)
        {
            char type = (char)data[p++];
            switch (type)
            {
                case 'Y':
                    p += 2;
                    return (double)BitConverter.ToInt16(data, p - 2);
                case 'C':
                    p += 1;
                    return (double)data[p - 1];
                case 'I':
                    p += 4;
                    return (double)BitConverter.ToInt32(data, p - 4);
                case 'F':
                    p += 4;
                    return (double)BitConverter.ToSingle(data, p - 4);
                case 'D':
                    p += 8;
                    return BitConverter.ToDouble(data, p - 8);
                case 'L':
                    p += 8;
                    return (double)BitConverter.ToInt64(data, p - 8);
                case 'S':
                case 'R':
                {
                    int length = BitConverter.ToInt32(data, p);
                    p += 4 + length;
                    return type == 'S' ? Encoding.UTF8.GetString(data, p - length, length) : (object)new byte[0];
                }

                default:
                {
                    int count = BitConverter.ToInt32(data, p);
                    int encoding = BitConverter.ToInt32(data, p + 4);
                    int length = BitConverter.ToInt32(data, p + 8);
                    p += 12;
                    byte[] raw = new byte[length];
                    Array.Copy(data, p, raw, 0, length);
                    p += length;
                    if (encoding == 1)
                    {
                        using var inflate = new ZLibStream(new MemoryStream(raw), CompressionMode.Decompress);
                        using var output = new MemoryStream();
                        inflate.CopyTo(output);
                        raw = output.ToArray();
                    }

                    switch (type)
                    {
                        case 'd':
                        {
                            var values = new double[count];
                            Buffer.BlockCopy(raw, 0, values, 0, count * 8);
                            return values;
                        }

                        case 'i':
                        {
                            var values = new int[count];
                            Buffer.BlockCopy(raw, 0, values, 0, count * 4);
                            return values;
                        }

                        case 'f':
                        {
                            var values = new float[count];
                            Buffer.BlockCopy(raw, 0, values, 0, count * 4);
                            return values;
                        }

                        default:
                            return raw;
                    }
                }
            }
        }
    }
}
