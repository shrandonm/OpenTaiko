using Silk.NET.OpenGLES;

namespace FDK;

public static class ShaderHelper
{
	public static uint CreateShader(string code, ShaderType shaderType)
	{
		uint vertexShader = Game.Gl.CreateShader(shaderType);

		Game.Gl.ShaderSource(vertexShader, code);

		Game.Gl.CompileShader(vertexShader);

		Game.Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int status);

		if (status != (int)GLEnum.True)
			throw new Exception($"{shaderType} failed to compile:{Game.Gl.GetShaderInfoLog(vertexShader)}");

		return vertexShader;
	}

	public static uint CreateShaderProgram(uint vertexShader, uint fragmentShader)
	{
		uint program = Game.Gl.CreateProgram();

		Game.Gl.AttachShader(program, vertexShader);
		Game.Gl.AttachShader(program, fragmentShader);

		Game.Gl.LinkProgram(program);

		Game.Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);

		if (linkStatus != (int)GLEnum.True)
			throw new Exception($"Program failed to link:{Game.Gl.GetProgramInfoLog(program)}");

		Game.Gl.DetachShader(program, vertexShader);
		Game.Gl.DetachShader(program, fragmentShader);

		return program;
	}

	public static uint CreateShaderProgramFromSource(string vertexCode, string fragmentCode)
	{
		uint vertexShader = CreateShader(vertexCode, ShaderType.VertexShader);
		uint fragmentShader = CreateShader(fragmentCode, ShaderType.FragmentShader);

		uint program = CreateShaderProgram(vertexShader, fragmentShader);

		Game.Gl.DeleteShader(vertexShader);
		Game.Gl.DeleteShader(fragmentShader);

		return program;
	}

	public static uint CreateFullscreenQuadShader()
	{
		string vertexCode = @"#version 300 es
precision mediump float;

layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aTexCoord;

out vec2 TexCoord;

void main()
{
    gl_Position = vec4(aPos, 0.0, 1.0); // clip-space position
    TexCoord = aTexCoord;               // pass texcoord to fragment shader
}
";

		string fragmentCode = @"#version 300 es
precision mediump float;

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D screenTexture;

void main()
{
    FragColor = texture(screenTexture, TexCoord);
}
";
		return CreateShaderProgramFromSource(vertexCode, fragmentCode);
	}
}
