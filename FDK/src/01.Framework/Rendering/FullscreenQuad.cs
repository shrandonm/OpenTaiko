using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core;
using Silk.NET.GLFW;
using Silk.NET.OpenGLES;
using System.Drawing;
using Silk.NET.Maths;

namespace FDK;

internal class FullscreenQuad
{
	public uint Shader { get; private set; }
	public uint VBO { get; private set; }
	public uint VAO { get; private set; }

	public FullscreenQuad()
	{
		GL gl = Game.Gl;
		Shader = ShaderHelper.CreateFullscreenQuadShader();

		float[] quadVertices =
		{
			// positions   // texcoords
			-1f, -1f,     0f, 0f,  // bottom left
			 1f, -1f,     1f, 0f,  // bottom right
			-1f,  1f,     0f, 1f,  // top left
			 1f,  1f,     1f, 1f   // top right
		};

		VAO = gl.GenVertexArray();
		VBO = gl.GenBuffer();

		gl.BindVertexArray(VAO);

		gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
		unsafe
		{
			fixed (float* data = quadVertices)
			{
				gl.BufferData(BufferTargetARB.ArrayBuffer,
					  (nuint)(quadVertices.Length * sizeof(float)),
					  data, BufferUsageARB.StaticDraw);
			}
		}

		// position attribute (location = 0)
		gl.EnableVertexAttribArray(0);
		gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

		// texcoord attribute (location = 1)
		gl.EnableVertexAttribArray(1);
		gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

		gl.BindVertexArray(0);
	}
}
