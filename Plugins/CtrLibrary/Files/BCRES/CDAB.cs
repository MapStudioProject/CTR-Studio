using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary.Bcres
{
    /// <summary>
    /// Represents a list of bounding boxes for sub meshes in a .bcmdl.
    /// The bounding data is checked inside a culling object (clip) from kmp with the linked route representing a region to cull.
    /// If a box is inside the clip region and is called by a checkpoint, it will unload the face descriptor from the .bcmdl.
    /// </summary>
    public class CDAB
    {
        /// <summary>
        /// The instance of the clip file.
        /// </summary>
        public static CDAB Instance = new CDAB();

        /// <summary>
        /// The list of shapes. Tyically this is always one.
        /// </summary>
        public List<Shape> Shapes = new List<Shape>();

        //Constant magics
        static readonly string HEADER_MAGIC = "BADC"; //CDAB
        static readonly string SHAPE_MAGIC = "PAHS"; //SHAP
        static readonly string STREAM_MAGIC = "MRTS"; //STRM

        //Fixed header size
        private readonly uint HEADER_SIZE = 16;

        //Unknown value (maybe version number)
        private uint UnkValue = 1000;

        public void Load(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                reader.ReadUInt32(); //magic
                reader.ReadUInt32(); //file size
                reader.ReadUInt32(); //header
                UnkValue = reader.ReadUInt32(); //unk

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if (magic != SHAPE_MAGIC)
                        throw new Exception();

                    Shape shape = new Shape();
                    Shapes.Add(shape);

                    uint numStreams = reader.ReadUInt32();
                    for (int i = 0; i < numStreams; i++)
                    {
                        MeshStream stream = new MeshStream();
                        stream.Read(reader);
                        shape.Streams.Add(stream);
                    }
                }
            }
        }

        public void Save(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(Encoding.ASCII.GetBytes(HEADER_MAGIC));
                writer.Write(0); //file size for later
                writer.Write(HEADER_SIZE); //header size
                writer.Write(UnkValue); //unk. Maybe version number

                //Write shapes then stream data
                foreach (var shape in Shapes)
                {
                    writer.Write(Encoding.ASCII.GetBytes(SHAPE_MAGIC));
                    writer.Write(shape.Streams.Count);

                    foreach (var stream in shape.Streams)
                        stream.Write(writer);
                }

                //File size
                writer.Seek(4, SeekOrigin.Begin);
                writer.Write((uint)writer.BaseStream.Length);
            }
        }

        public class Shape
        {
            /// <summary>
            /// A list of mesh streams assigned per bcmdl mesh.
            /// </summary>
            public List<MeshStream> Streams = new List<MeshStream>();
        }

        public class MeshStream
        {
            /// <summary>
            /// The total vertex count used by the bcmdl mesh.
            /// </summary>
            public ushort VertexCount;

            /// <summary>
            /// A list of sub mesh boundings, assigned per each bcmdl shape face descriptor.
            /// </summary>
            public List<BoundingBox> Boundings = new List<BoundingBox>();

            public void Read(BinaryReader reader)
            {
                string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (magic != STREAM_MAGIC)
                    throw new Exception();

                ushort numBoundings = reader.ReadUInt16();
                VertexCount = reader.ReadUInt16();
                for (int i = 0; i < numBoundings; i++)
                {
                    Boundings.Add(new BoundingBox()
                    {
                        MinX = reader.ReadSingle(),
                        MinZ = reader.ReadSingle(),
                        MaxX = reader.ReadSingle(),
                        MaxZ = reader.ReadSingle(),
                    });
                }
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(Encoding.ASCII.GetBytes(STREAM_MAGIC));
                writer.Write((ushort)Boundings.Count);
                writer.Write(VertexCount);
                foreach (var bb in Boundings)
                {
                    writer.Write(bb.MinX);
                    writer.Write(bb.MinZ);
                    writer.Write(bb.MaxX);
                    writer.Write(bb.MaxZ);
                }
            }
        }

        /// <summary>
        /// Represents a 2D bounding box for checking intersections with clips.
        /// </summary>
        public class BoundingBox
        {
            public float MinX;
            public float MaxX;
            public float MinZ;
            public float MaxZ;
        }
    }
}
