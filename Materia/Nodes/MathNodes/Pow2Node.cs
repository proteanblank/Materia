﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Materia.MathHelpers;

namespace Materia.Nodes.MathNodes
{
    public class Pow2Node : MathNode
    {
        NodeInput input;
        NodeOutput output;

        public Pow2Node(int w, int h, GraphPixelType p = GraphPixelType.RGBA)
        {
            //we ignore w,h,p

            CanPreview = false;

            Name = "Pow2";
            Id = Guid.NewGuid().ToString();
            shaderId = "S" + Id.Split('-')[0];


            input = new NodeInput(NodeType.Float, this, "Float Input");
            output = new NodeOutput(NodeType.Float, this);

            Inputs = new List<NodeInput>();
            Inputs.Add(input);

            input.OnInputAdded += Input_OnInputAdded;
            input.OnInputChanged += Input_OnInputChanged;

            Outputs = new List<NodeOutput>();
            Outputs.Add(output);
        }

        private void Input_OnInputChanged(NodeInput n)
        {
            TryAndProcess();
        }

        private void Input_OnInputAdded(NodeInput n)
        {
            TryAndProcess();
        }

        public override void TryAndProcess()
        {
            if (input.HasInput)
            {
                Process();
            }
        }

        public override string GetShaderPart()
        {
            if (!input.HasInput) return "";
            var s = shaderId + "0";
            var n1id = (input.Input.Node as MathNode).ShaderId;

            var index = input.Input.Node.Outputs.IndexOf(input.Input);

            n1id += index;

            return "float " + s + " = pow(2," + n1id + ");\r\n";
        }

        void Process()
        {
            if (input.Input.Data == null) return;

            object o = input.Input.Data;

            if (o is float || o is int)
            {
                float v = (float)o;

                output.Data = (float)Math.Pow(2, v);
                output.Changed();
            }
            else
            {
                output.Data = 0;
                output.Changed();
            }

            if (ParentGraph != null)
            {
                FunctionGraph g = (FunctionGraph)ParentGraph;

                if (g != null && g.OutputNode == this)
                {
                    g.Result = output.Data;
                }
            }
        }
    }
}
