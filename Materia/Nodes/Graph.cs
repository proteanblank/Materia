﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;
using System.Windows;
using Materia.Nodes.Atomic;
using Materia.Nodes.Attributes;
using OpenTK.Graphics.OpenGL;

namespace Materia.Nodes
{
    public enum GraphPixelType
    {
        RGBA = PixelInternalFormat.Rgba8,
        RGBA16F = PixelInternalFormat.Rgba16f,
        RGBA32F = PixelInternalFormat.Rgba32f,
        RGB = PixelInternalFormat.Rgb8,
        RGB16F = PixelInternalFormat.Rgb16f,
        RGB32F = PixelInternalFormat.Rgb32f,
        Luminance16F = PixelInternalFormat.R16f,
        Luminance32F = PixelInternalFormat.R32f,
    }

    public class Graph : IDisposable
    {
        public delegate void GraphUpdate(Graph g);
        public event GraphUpdate OnGraphUpdated;

        [HideProperty]
        public bool ReadOnly { get; set; }
        [HideProperty]
        public string CWD { get; set; }
        public List<Node> Nodes { get; protected set; }
        public Dictionary<string, Node> NodeLookup { get; protected set; }
        public List<string> OutputNodes { get; protected set; }
        public List<string> InputNodes { get; protected set; }

        protected Dictionary<string, object> Variables { get; set; }

        protected Dictionary<string, Point> OriginSizes;

        [HideProperty]
        public string Name { get; set; }

        protected GraphPixelType defaultTextureType;

        [Dropdown(null)]
        [Title(Title = "Default Texture Type")]
        public GraphPixelType DefaultTextureType
        {
            get
            {
                return defaultTextureType;
            }
            set
            {
                if (!ReadOnly)
                {
                    defaultTextureType = value;
                }
            }
        }

        protected int randomSeed;

        [Title(Title = "Random Seed")]
        public int RandomSeed
        {
            get
            {
                return randomSeed;
            }
            set
            {
                randomSeed = value;
                TryAndProcess();
            }
        }

        public class GPoint
        {
            public double x;
            public double y;

            public GPoint()
            {

            }

            public GPoint(double x, double y)
            {
                this.x = x;
                this.y = y;
            }

            public GPoint(Point p)
            {
                x = p.X;
                y = p.Y;
            }

            public Point ToPoint()
            {
                return new Point(x, y);
            }
        }

        protected int width;
        protected int height;
        [Slider(IsInt = true, Max = 4096, Min = 16, Snap = true, Ticks = new float[] { 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 })]
        [Section(Section = "Standard")]
        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                if(!ReadOnly)
                    width = value;
            }
        }
        [Slider(IsInt = true, Max = 4096, Min = 16, Snap = true, Ticks = new float[] { 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 })]
        [Section(Section = "Standard")]
        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                if(!ReadOnly)
                    height = value;
            }
        }

        public Graph(string name, int w = 256, int h = 256)
        {
            Name = name;
            width = w;
            height = h;
            Variables = new Dictionary<string, object>();
            defaultTextureType = GraphPixelType.RGBA;
            Nodes = new List<Node>();
            NodeLookup = new Dictionary<string, Node>();
            OutputNodes = new List<string>();
            InputNodes = new List<string>();
            OriginSizes = new Dictionary<string, Point>();
        }

        public virtual T GetVar<T>(string k)
        {
            T v = default(T);

            if(Variables.ContainsKey(k))
            {
                v = (T)Variables[k];
            }

            return v;
        }

        public virtual void RemoveVar(string k)
        {
            Variables.Remove(k);
        }

        public virtual void SetVar(string k, object v)
        {
            Variables[k] = v;
        }

        public class GraphData
        {
            public string name;
            public List<string> nodes;
            public List<string> outputs;
            public List<string> inputs;
            public GraphPixelType defaultTextureType;
        }

        public virtual void TryAndProcess()
        {
            foreach (Node n in Nodes)
            {
                if (OutputNodes.Contains(n.Id))
                {
                    continue;
                }
                if (InputNodes.Contains(n.Id))
                {
                    continue;
                }

                n.TryAndProcess();
            }

            //gather inputs
            foreach (string iid in InputNodes)
            {
                Node n;
                if (NodeLookup.TryGetValue(iid, out n))
                {
                    InputNode inp = (InputNode)n;
                    inp.TryAndProcess();
                }
            }
        }

        /// <summary>
        /// this is used in GraphInstances
        /// To resize proportionate to new size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public virtual void ResizeWith(int width, int height)
        {
            this.width = width;
            this.height = height;

            foreach(Node n in Nodes)
            {
                if(OutputNodes.Contains(n.Id))
                {
                    continue;
                }
                if(InputNodes.Contains(n.Id))
                {
                    continue;
                }

                if (n is BitmapNode)
                {
                    BitmapNode bn = (BitmapNode)n;
                    bn.TryAndProcess();
                }
                else
                {
                    Point osize;

                    if (OriginSizes.TryGetValue(n.Id, out osize))
                    {
                        float wratio = width / (float)osize.X;
                        float hratio = height / (float)osize.Y;

                        int fwidth = (int)Math.Min(4096, Math.Max(16, Math.Round(osize.X * wratio)));
                        int fheight = (int)Math.Min(4096, Math.Max(16, Math.Round(osize.Y * hratio)));

                        n.Width = fwidth;
                        n.Height = fheight;
                    }
                }
            }

            //gather inputs
            foreach (string iid in InputNodes)
            {
                Node n;
                if (NodeLookup.TryGetValue(iid, out n))
                {
                    InputNode inp = (InputNode)n;
                    inp.TryAndProcess();
                }
            }
        }

        public virtual string GetJson()
        {
            GraphData d = new GraphData();

            List<string> data = new List<string>();

            foreach(Node n in Nodes)
            {
                data.Add(n.GetJson());
            }
            d.name = Name;
            d.nodes = data;
            d.outputs = OutputNodes;
            d.inputs = InputNodes;
            d.defaultTextureType = defaultTextureType;

            return JsonConvert.SerializeObject(d);
        }

        public virtual bool Add(Node n)
        {
            if (NodeLookup.ContainsKey(n.Id)) return false;

            if(n is OutputNode)
            {
                OutputNodes.Add(n.Id);
            }
            else if(n is InputNode)
            {
                InputNodes.Add(n.Id);
            }

            NodeLookup[n.Id] = n;
            Nodes.Add(n);

            n.OnUpdate += N_OnUpdate;

            return true;
        }

        private void N_OnUpdate(Node n)
        {
            Updated();
        }

        /// <summary>
        /// This is used in GraphInstanceNodes
        /// We only save the final buffers connected to the outputs,
        /// and release all other buffers to save video card memory
        /// since it is all in video memory and shader based
        /// we do not have to transfer data to the video card
        /// so it will be relatively fast still to update
        /// when we have to recreate the textures
        /// </summary>
        public virtual void ReleaseIntermediateBuffers()
        {
            foreach(Node n in Nodes)
            {
                if(OutputNodes.Contains(n.Id))
                {
                    continue;
                }

                if(n.Buffer != null)
                {
                    n.Buffer.Release();
                }
            }
        }

        public virtual void Remove(Node n)
        {
            if(n is OutputNode)
            {
                OutputNodes.Remove(n.Id);
            }
            else if(n is InputNode)
            {
                InputNodes.Remove(n.Id);
            }

            NodeLookup.Remove(n.Id);
            if(Nodes.Remove(n))
            {
                n.OnUpdate -= N_OnUpdate;
            }
            n.Dispose();
        }

        public virtual Node CreateNode(string type)
        {
            if(ReadOnly)
            {
                return null;
            }

            if(type.Contains(System.IO.Path.PathSeparator))
            {
                var n = new GraphInstanceNode(width, height);
                n.ParentGraph = this;
                return n;
            }

            try
            {
                Type t = Type.GetType(type);
                if(t != null)
                {
                    if (t.Equals(typeof(OutputNode)))
                    {
                        var n  = new OutputNode(defaultTextureType);
                        n.ParentGraph = this;
                        return n;
                    }
                    else if(t.Equals(typeof(InputNode)))
                    {
                        var n = new InputNode(defaultTextureType);
                        n.ParentGraph = this;
                        return n;
                    }
                    else
                    {
                        Node n = (Node)Activator.CreateInstance(t, width, height, defaultTextureType);
                        n.ParentGraph = this;
                        return n;
                    }
                }
                else
                {
                    var n = new GraphInstanceNode(width, height);
                    n.ParentGraph = this;
                    return n;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        public virtual void FromJson(string data)
        {
            GraphData d = JsonConvert.DeserializeObject<GraphData>(data);

            if (d != null)
            {
                Dictionary<string, Node> lookup = new Dictionary<string, Node>();
                Dictionary<string, string> nodeData = new Dictionary<string, string>();

                Name = d.name;
                OutputNodes = d.outputs;
                InputNodes = d.inputs;
                defaultTextureType = d.defaultTextureType;

                //parse node data
                //setup initial object instances
                foreach (string s in d.nodes)
                {
                    Node.NodeData nd = JsonConvert.DeserializeObject<Node.NodeData>(s);

                    if (nd != null)
                    {
                        string type = nd.type;
                        if (!string.IsNullOrEmpty(type))
                        {
                            try
                            {
                                Type t = Type.GetType(type);
                                if (t != null)
                                {
                                    //special case to handle output nodes
                                    if (t.Equals(typeof(OutputNode)))
                                    {
                                        OutputNode n = new OutputNode(defaultTextureType);
                                        n.ParentGraph = this;
                                        n.Id = nd.id;
                                        lookup[nd.id] = n;
                                        Nodes.Add(n);
                                        nodeData[nd.id] = s;
                                    }
                                    else if(t.Equals(typeof(InputNode)))
                                    {
                                        InputNode n = new InputNode(defaultTextureType);
                                        n.ParentGraph = this;
                                        n.Id = nd.id;
                                        lookup[nd.id] = n;
                                        Nodes.Add(n);
                                        nodeData[nd.id] = s;
                                    }
                                    else
                                    {
                                        Node n = (Node)Activator.CreateInstance(t, nd.width, nd.height, defaultTextureType);
                                        if (n != null)
                                        {
                                            n.ParentGraph = this;
                                            n.Id = nd.id;
                                            lookup[nd.id] = n;
                                            Nodes.Add(n);
                                            nodeData[nd.id] = s;
                                        }
                                    }
                                }
                                else
                                {
                                    //log we could not load graph node
                                }
                            }
                            catch
                            {
                                //log we could not load graph node
                            }
                        }
                    }
                }

                NodeLookup = lookup;

                //apply data to nodes
                foreach(Node n in Nodes)
                {
                    string ndata = null;
                    nodeData.TryGetValue(n.Id, out ndata);

                    if(!string.IsNullOrEmpty(ndata))
                    {
                        n.FromJson(lookup, ndata);

                        //origin sizes are only for graph instances
                        //not actually used in the current one being edited
                        //it is used in the ResizeWith
                        OriginSizes[n.Id] = new Point(n.Width, n.Height);
                    }
                }
            }
        }

        public void CopyResources(string cwd)
        {
            foreach (Node n in Nodes)
            {
                n.CopyResources(cwd);
            }

            //set last in case we need to copy from current graph cwd to new cwd
            this.CWD = cwd;
        }

        public virtual void Dispose()
        {
            if (Nodes != null)
            {
                foreach (Node n in Nodes)
                {
                    n.Dispose();
                }

                Nodes.Clear();
                Nodes = null;
            }
        }

        protected void Updated()
        {
            if(OnGraphUpdated != null)
            {
                OnGraphUpdated.Invoke(this);
            }
        }
    }
}
