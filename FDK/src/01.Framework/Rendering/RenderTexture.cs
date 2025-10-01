using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core;
using Silk.NET.GLFW;
using Silk.NET.OpenGLES;

namespace FDK;

internal class RenderTexture : IDisposable
{
	public uint FrameBufferObject { get; private set; }
	public uint Texture { get; private set; }
	public uint VertexBufferObject { get; private set; }
	public uint VertexArrayObject { get; private set; }
	private uint m_Width;
	private uint m_Height;

	public RenderTexture(uint width, uint height)
	{
		m_Width = width;
		m_Height = height;
		unsafe
		{
			Texture = CTexture.GenTexture(null, width, height, PixelFormat.Rgba);
		}
		FrameBufferObject = CreateFrameBufferObject(Texture, width, height);

		GL gl = Game.Gl;
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
	}

	~RenderTexture()
	{
		Dispose();
	}

	public void Dispose()
	{
		GL gl = Game.Gl;
		if (FrameBufferObject != 0)
			gl.DeleteFramebuffer(FrameBufferObject);

		if (Texture != 0)
			gl.DeleteTexture(Texture);

		if (VertexBufferObject != 0)
			gl.DeleteBuffer(VertexBufferObject);

		if (VertexArrayObject != 0)
			gl.DeleteVertexArray(VertexArrayObject);
	}

	private static uint CreateFrameBufferObject(uint texture, uint width, uint height)
	{
		GL gl = Game.Gl;
		uint frameBufferObject = gl.GenFramebuffer();
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferObject);

		gl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
			FramebufferAttachment.ColorAttachment0,
			TextureTarget.Texture2D,
			texture, level: 0);

		var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
		if (status != GLEnum.FramebufferComplete)
		{
			throw new Exception($"Frame buffer incomplete: {status}");
		}
		return frameBufferObject;
	}

	public void Bind()
	{
		Game.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferObject);
		Game.Gl.Viewport(0, 0, m_Width, m_Height);
	}

	public void Unbind()
	{
		Game.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
	}
}
