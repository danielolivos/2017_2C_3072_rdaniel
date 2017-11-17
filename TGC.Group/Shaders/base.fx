//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))
float4x4 matProj;			// Projection
float specularFactor = 3;

static const float PI = 3.14159265f;

int ssao = 1;
float screen_dx;					// tamaño de la pantalla en pixels
float screen_dy;
float time;


//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
	Texture = (texDiffuseMap);
	ADDRESSU = MIRROR;
	ADDRESSV = MIRROR;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

//Textura para Lightmap
texture texLightMap;
sampler2D lightMap = sampler_state
{
	Texture = (texLightMap);
};


texture g_RenderTarget;
sampler RenderTarget =
sampler_state
{
	Texture = <g_RenderTarget>;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

texture g_RenderTarget4;
sampler RenderTarget4 =
sampler_state
{
	Texture = <g_RenderTarget4>;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

texture g_Position;
sampler PositionBuffer =
sampler_state
{
	Texture = <g_Position>;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};


texture g_Normal;
sampler NormalBuffer =
sampler_state
{
	Texture = <g_Normal>;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};


texture texNoise;
sampler2D noise = sampler_state
{
	Texture = (texNoise);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = POINT;
	MAGFILTER = POINT;
	MIPFILTER = POINT;
};

texture texDeathStarSurface;
sampler2D ds_surface = sampler_state
{
	Texture = (texDeathStarSurface);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

texture texSkybox;
sampler2D SkyBoxMap = sampler_state
{
	Texture = (texSkybox);
	ADDRESSU = MIRROR;
	ADDRESSV = MIRROR;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};



//Material del mesh
float3 materialEmissiveColor; //Color RGB
float3 materialAmbientColor; //Color RGB
float4 materialDiffuseColor; //Color ARGB (tiene canal Alpha)
float3 materialSpecularColor; //Color RGB
float materialSpecularExp; //Exponente de specular

//Parametros de la Luz
float3 lightColor; //Color RGB de la luz
float4 lightPosition; //Posicion de la luz
float4 eyePosition; //Posicion de la camara
float lightIntensity; //Intensidad de la luz
float lightAttenuation; //Factor de atenuacion de la luz
float3 lightDir; 		//Luz direccional

struct VS_INPUT
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float4 Color : COLOR;
	float2 Texcoord : TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float4 WorldNormalOcc : TEXCOORD2;			// xyz = normal w = off factor
	float3 ViewVec	: TEXCOORD3;
	float3 LightVec	: TEXCOORD4;

};

VS_OUTPUT vs_main(VS_INPUT input)
{
	VS_OUTPUT output;
	output.Position = mul(input.Position, matWorldViewProj);
	output.Texcoord = input.Texcoord;
	output.WorldPosition = mul(input.Position, matWorld);
	output.ViewVec = eyePosition.xyz - output.WorldPosition;				//ViewVec (V): vector que va desde el vertice hacia la camara.
	output.LightVec = lightPosition.xyz - output.WorldPosition;
	// normal en worldspace
	output.WorldNormalOcc.xyz = mul(input.Normal, matInverseTransposeWorld).xyz;
	// factor de occlussion
	output.WorldNormalOcc.w = 0.5 + input.Position.y/10.0;
	return output;
}

struct PS_INPUT
{
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float4 WorldNormalOcc : TEXCOORD2;
	float3 ViewVec	: TEXCOORD3;
	float3 LightVec	: TEXCOORD4;
};

//Pixel Shader
float4 ps_main(PS_INPUT input) : COLOR0
{
	float occ_factor = ssao ? input.WorldNormalOcc.w : 1;
	float3 Nn = normalize(input.WorldNormalOcc.xyz);
	float3 Ln = normalize(input.LightVec);
	//float3 Ln = lightDir;
	float3 Vn = normalize(input.ViewVec);
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);
	float3 ambientLight = lightColor * materialAmbientColor * occ_factor;
	float3 n_dot_l = dot(Nn, Ln);
	float3 diffuseLight = lightColor * materialDiffuseColor.rgb * max(0.0, n_dot_l);
	float ks = saturate(dot(reflect(-Ln,Nn), Vn));
	float3 specularLight = specularFactor * lightColor * materialSpecularColor *pow(ks,materialSpecularExp);
	float4 finalColor = float4(saturate(materialEmissiveColor + ambientLight + diffuseLight) * texelColor + specularLight, materialDiffuseColor.a);
	return finalColor  * occ_factor;
	
}


float4 ps_normal(PS_INPUT input) : COLOR0
{
	float3 Nn = normalize(input.WorldNormalOcc.xyz);
	return float4(Nn, 1);
}


technique DefaultTechnique
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_main();
	}
}


technique NormalMap
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_normal();
	}
}


void VSCopy(float4 vPos : POSITION, float2 vTex : TEXCOORD0, out float4 oPos : POSITION, out float2 oScreenPos : TEXCOORD0)
{
	oPos = vPos;
	oScreenPos = vTex;
	oPos.w = 1;
}



float4 texCube_skybox(float3 d)
{
	float3 absd = abs(d);
	float s0 = 0; 
	float t0 = 0; 
	float s,t;
	float sc, tc, ma;

	if ((absd.x >= absd.y) && (absd.x >= absd.z)) 
	{
		if (d.x > 0.0f) 
		{
			// right
			s0 = 0.5 , t0 = 1.0/3.0;
			sc = -d.z; tc = -d.y; ma = absd.x;
		} 
		else 
		{
			// left
			s0 = 0 , t0 = 1.0/3.0;
			sc = d.z; tc = -d.y; ma = absd.x;
		}
	}
	if ((absd.y >= absd.x) && (absd.y >= absd.z)) 
	{
		if (d.y > 0.0f) 
		{
			// top
			s0 = 0.25 , t0 = 0;
			sc = d.x; tc = d.z; ma = absd.y;
		} 
		else 
		{
			// bottom
			s0 = 0.25 , t0 = 2.0/3.0;
			sc = d.x; tc = -d.z; ma = absd.y;
		}
	}
	if ((absd.z >= absd.x) && (absd.z >= absd.y)) 
	{
		if (d.z > 0.0f) 
		{
			// front
			s0 = 0.25 , t0 = 1.0/3.0;
			sc = d.x; tc = -d.y; ma = absd.z;
		} 
		else 
		{
			// back
			s0 = 0.75 , t0 = 1.0/3.0;
			sc = -d.x; tc = -d.y; ma = absd.z;
		}
	}

	if (ma == 0.0f) 
	{
		s = 0.0f;
		t = 0.0f;
	} 
	else 
	{
		s = ((sc / ma) + 1.0f) * 0.5f;
		t = ((tc / ma) + 1.0f) * 0.5f;
	}
	float ep = 0.01f;
	s = clamp(s , ep , 1-ep);
	t = clamp(t , ep , 1-ep);
	return tex2Dlod(SkyBoxMap, float4(s0+ s /4.0 ,t0+ t/3.0,0,0));
}

float3 LookFrom;
float3 ViewDir;
float3 Dx , Dy;
float MatProjQ, Zn,Zf;

struct PS_OUTPUT 
{
	float4 color : COLOR0;
	float depth : DEPTH0;
};

PS_OUTPUT ps_skybox(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) 
{
	PS_OUTPUT rta;
	float x = vpos.x;
	float y = vpos.y;
	float3 rd = normalize(ViewDir + Dy*(0.5*(screen_dy-2*y)) - Dx*(0.5*(2*x-screen_dx)));	
	rta.color = texCube_skybox(rd);
	rta.depth = 1;
	return rta;
}

technique SkyBox
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 ps_skybox();
	}
}

// Factor de distorcion del ojo de pez
float fish_kU = 0.25f; 


float4 PSPostProcess(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) : COLOR0
{
	float2 center = float2(0.5,0.5);
	float dist = distance(center, Tex);
    Tex -= center;
	float percent = 1.0 - ((0.5 - dist) / 0.5) * fish_kU;
	Tex *= percent;
    Tex += center;
	float2 TexG = Tex + float2(1.0/screen_dx , 1.0/screen_dy)*8.0;
	return tex2D(RenderTarget, Tex) + tex2D(RenderTarget4, TexG)*8.0;
}

technique PostProcess
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 PSPostProcess();
	}
}

float4 PSDownFilter4( in float2 Tex : TEXCOORD0 ) : COLOR0
{
    float4 Color = 0;
    for (int i = 0; i < 4; i++)
    for (int j = 0; j < 4; j++)
	{
		float4 c = tex2D(RenderTarget, Tex+float2((float)i/screen_dx,(float)j/screen_dy));
		if(c.r>0.3 && c.g<0.05 && c.b<0.05)
			Color = float4(1,0,0,1);
	}

	return Color;
}



technique DownFilter4
{
   pass Pass_0
   {
	  VertexShader = compile vs_3_0 VSCopy();
	  PixelShader = compile ps_3_0 PSDownFilter4();
   }

}

// Gaussian Blur

static const int kernel_r = 6;
static const int kernel_size = 13;
static const float kernel_escale = 4.0;

static const float  inv_kernel_size = 1.0/13.0;
static const float Kernel[kernel_size] = 
{
    0.002216,    0.008764,    0.026995,    0.064759,    0.120985,    0.176033,    0.199471,    0.176033,    0.120985,    0.064759,    0.026995,    0.008764,    0.002216,
};

void BlurH(float2 screen_pos  : TEXCOORD0,out float4 Color : COLOR)
{ 
    Color = 0;
	for(int i=0;i<kernel_size;++i)
		Color += tex2D(RenderTarget, screen_pos+float2((float)(i-kernel_r)/screen_dx,0)*kernel_escale) *inv_kernel_size;
	Color.a = 1;
}

void BlurV(float2 screen_pos  : TEXCOORD0,out float4 Color : COLOR)
{ 
    Color = 0;
	for(int i=0;i<kernel_size;++i)
		Color += tex2D(RenderTarget, screen_pos+float2(0,(float)(i-kernel_r)/screen_dy)*kernel_escale) *inv_kernel_size;
	Color.a = 1;

}

technique GaussianBlurSeparable
{
   pass Pass_0
   {
	  VertexShader = compile vs_3_0 VSCopy();
	  PixelShader = compile ps_3_0 BlurH();
   }
   pass Pass_1
   {
	  VertexShader = compile vs_3_0 VSCopy();
	  PixelShader = compile ps_3_0 BlurV();
   }

}

