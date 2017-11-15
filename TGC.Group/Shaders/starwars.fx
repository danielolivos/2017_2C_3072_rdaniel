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



float4 PSPostProcess(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) : COLOR0
{
	return tex2D(RenderTarget, Tex);
}

technique PostProcess
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 PSPostProcess();
	}
}



const float star_r = 8000;
const float r2 = 8000 * 8000;
const float3 reactor = float3(-5600,5600,0);
const float radio_reactor = 3000;


float2 sphere(float3 ro, float3 rd , float3 center, float radio)
{
	ro-=center;
	float c = dot(ro, ro) - radio*radio;
	float b = dot(rd, ro);
	float d = b*b - c;
	return d<0 ? float2(0,0) : float2 (-b - sqrt(d) , -b + sqrt(d));
}

float disc(float3 ro, float3 rd , float3 center, float radio)
{
	float rta = 0;
	float t = (center.y - ro.y) / rd.y;
	if(t>0)
	{	
		float3 Ip = ro + t*rd;
		rta = length(Ip - center)< radio ? t : 0;
	}
	return rta;
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


PS_OUTPUT ps_ds(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) 
{
	PS_OUTPUT rta;
	float x = vpos.x;
	float y = vpos.y;
	float3 rd = normalize(ViewDir + Dy*(0.5*(screen_dy-2*y)) - Dx*(0.5*(2*x-screen_dx)));	
	float4 sky_color = texCube_skybox(rd);
	
	float3 color_base = 0;
	float3 N = 0;
	int lighting = 1;
	float3 Ip = 0;
	float2 q = sphere(LookFrom, rd ,float3(0,0,0) , star_r);
	float t = q.x;
	float t0 = q.x;		// q.x = t de entrada a la esfera, todos los puntos de interseccion tienen que ser luego de t0
	float tf = q.y;		// q.y = t de salida 
	float phi = 0 , theta = 0;
	bool en_reactor = false;
	
	const float k_theta = 2400;
	const float k_phi = 800;
	
	if(t>0)
	{
		Ip = LookFrom + rd * t;
		if(length(Ip - reactor)<radio_reactor)
		{
			en_reactor = true;
			float t0 = sphere(LookFrom, rd  , reactor , radio_reactor).y;
			t = t0;
			Ip = LookFrom + rd * t;
			if(dot(Ip,Ip)>r2)
				t = 0;
			else
			{
				color_base.rgb = 0.25;
				N = normalize(reactor - Ip);
			}
		}
		else
		{
			N = normalize(Ip);
			theta = atan2(N.x, N.z) / (2*PI) + 0.5;
			phi = N.y * 0.5 + 0.5;			
			
			float2 tx = round(float2(theta,phi)*1600)/20;
			float2 ruido = round(tex2D(noise,tx).xy * 256);
			tx = float2(theta*k_theta,phi*k_phi) + ruido*0.25;
			color_base = tex2D(ds_surface, tx).rgb;
			color_base *= 1 - (star_r - length(Ip))/45.0;
		}
	}
	else
	if(q.y>0 && t<0)
	{
		// el punto de vista esta dentro de la esfera
		t0 = 0;
		Ip = LookFrom;
	}

	float dty = abs(Ip.y)<100 ? 30 : 15;
	float dtx = 15;
	float dq = 400;
	bool trench_y =	fmod(Ip.y +80000+dty, dq)<2*dty && abs(Ip.y)<2000 && 
		(abs(Ip.x)<2000 || abs(Ip.y)<100) ? true : false;
	bool trench_x =	fmod(Ip.x +80000+dtx, dq)<2*dtx && abs(Ip.x)<2000 && abs(Ip.y)<2000 ? true : false;
	if(trench_x || trench_y)
		t = 0;

		
	for(int k=-5;k<=5;++k)
	{
		float h = k*dq;
		float t1 = (h-dty-LookFrom.y) / rd.y;
		if(t1>t0 && t1<tf && (t1<t || t<=0))
		{
			Ip = LookFrom + rd * t1;
			if(!(fmod(Ip.x +80000+dtx, dq)<2*dtx && abs(Ip.x)<2000))
			{
				t = t1;
				float theta = atan2(Ip.x, Ip.z) / (2*PI) + 0.5;
				float phi = length(Ip.xz)/star_r;
				float2 tx = round(float2(theta,phi)*1600)/20;
				float2 ruido = round(tex2D(noise,tx).xy * 256);
				tx = float2(theta*k_theta,phi*k_phi) + ruido*0.25;
				color_base = tex2D(ds_surface, tx).rgb;
				color_base *= 1 - (star_r - length(Ip))/45.0;
				N = float3(0,1,0);
			}
		}
			
		t1 = (h+dty -LookFrom.y) / rd.y;
		if(t1>t0 && t1<tf && (t1<t || t<=0))
		{
			Ip = LookFrom + rd * t1;
			if(!(fmod(Ip.x +80000+dtx, dq)<2*dtx && abs(Ip.x)<2000))
			{
				t = t1;
				float theta = atan2(Ip.x, Ip.z) / (2*PI) + 0.5;
				float phi = length(Ip.xz)/star_r;
				float2 tx = round(float2(theta,phi)*1600)/20;
				float2 ruido = round(tex2D(noise,tx).xy * 256);
				tx = float2(theta*k_theta,phi*k_phi) + ruido*0.25;
				color_base = tex2D(ds_surface, tx).rgb;
				color_base *= 1 - (star_r - length(Ip))/45.0;
				N = float3(0,1,0);
			}
		}
		
		t1 = (h-dtx-LookFrom.x) / rd.x;
		if(t1>t0 && t1<tf && (t1<t || t<=0))
		{
			Ip = LookFrom + rd * t1;
			if(!(fmod(Ip.y +80000+dty, dq)<2*dty && abs(Ip.y)<2000))
			{
				t = t1;
				float theta = atan2(Ip.y, Ip.z) / (2*PI) + 0.5;
				float phi = length(Ip.yz)/star_r;
				float2 tx = round(float2(theta,phi)*1600)/20;
				float2 ruido = round(tex2D(noise,tx).yz * 256);
				tx = float2(theta*k_theta,phi*k_phi) + ruido*0.25;
				color_base = tex2D(ds_surface, tx).rgb;
				color_base *= 1 - (star_r - length(Ip))/45.0;
				N = float3(0,1,0);
			}
		}
		
		t1 = (h+dtx -LookFrom.x) / rd.x;
		if(t1>t0 && t1<tf && (t1<t || t<=0))
		{
			Ip = LookFrom + rd * t1;
			if(!(fmod(Ip.y +80000+dty, dq)<2*dty && abs(Ip.y)<2000))
			{
				t = t1;
				float theta = atan2(Ip.y, Ip.z) / (2*PI) + 0.5;
				float phi = length(Ip.yz)/star_r;
				float2 tx = round(float2(theta,phi)*1600)/20;
				float2 ruido = round(tex2D(noise,tx).yz * 256);
				tx = float2(theta*k_theta,phi*k_phi) + ruido*0.25;
				color_base = tex2D(ds_surface, tx).rgb;
				color_base *= 1 - (star_r - length(Ip))/45.0;
				N = float3(0,1,0);
			}
		}
		
	}
	
	if(!en_reactor)
	{
		float t1 = sphere(LookFrom, rd ,float3(0,0,0) , star_r-30).x;
		if(t1>t0 && t1<tf && (t1<t || t<=0))
		{
			t = t1;
			Ip = LookFrom + rd * t;
			N = normalize(Ip);
			theta = atan2(N.x, N.z) / (2*PI) + 0.5;
			phi = N.y * 0.5 + 0.5;			
			float2 tx = round(float2(theta,phi)*1600)/20;
			float2 ruido = round(tex2D(noise,tx).xy * 256);
			tx = float2(theta*k_theta,phi*k_phi) + ruido*0.25;
			color_base = tex2D(ds_surface, tx).rgb;
			color_base *= 1 - (star_r - length(Ip))/45.0;
		}
	}
	
	
		
	float3 L = normalize(lightPosition.xyz - Ip);
	float k = lighting ? clamp( abs(dot(N, L)) + 0.5 , 0, 1) : 1;
	rta.color = float4(color_base * k , 1);
	
	float Z = clamp(Zn + t , Zn,Zf);
	rta.depth = MatProjQ * (1-Zn / Z);
	
	if(t<=0)
	{
		rta.color = sky_color;
		rta.depth = 1;
	}
	
	return rta;
}


technique DeathStar
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 ps_ds();
	}
}


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

