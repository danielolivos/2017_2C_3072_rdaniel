using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Core.Utils;
using System.Collections.Generic;
using TGC.Core.Shaders;
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;
using System;
using TGC.Core.Terrain;
using System.Windows.Forms;

namespace TGC.Group.Model
{

    static class defines
    {
        public const int MODO_CAMARA = 0;
        public const int MODO_GAME = 1;
        public const int MODO_TEST_BLOCK = 2;
    }

    public class Block
    {
        private static readonly Random random = new Random();
        public static int tipo_o  = 0;
        public static float k = 0.11f;
        public static int r = 8;
        public static float largo = 10 * 2 * (r);
        public static float ancho = 10 * 2 * (r);
        public static float alto = 20;
        public Vector3 Position;
        public Matrix Orient;
        public Matrix MatPos;
        public Vector3 escale = new Vector3(1, 1.82f, 1) * k;
        public Matrix[] matWorld = new Matrix[500];
        public int[] mesh_index = new int[500];
        public Vector3[] pmin = new Vector3[500];
        public Vector3[] pmax = new Vector3[500];
        public int cant_mesh;
        public TgcBoundingAxisAlignBox BoundingBox;
        public GameModel model;
        public Matrix matWorldBock = new Matrix();
        public Matrix matWorldSurfaceBlock = new Matrix();
        public int tipo;
        public Matrix matWorldBlock;            // matrix word de todo el block
        public Matrix matInvWorldBlock;
        public int cant_obstaculos = 3;


        public static Matrix OrientTecho = Helper.MatrixfromBasis(1, 0, 0,
                                                        0, -1, 0,
                                                        0, 0, 1);

        public static Matrix OrientPared = Helper.MatrixfromBasis(1, 0, 0,
                                                        0, 0, -1,
                                                        0, 1, 0);

        public static Matrix OrientParedI = Helper.MatrixfromBasis(1, 0, 0,
                                                        0, 0, 1,
                                                        0, 1, 0);

        public static Matrix OrientParedU = Helper.MatrixfromBasis(0, 1, 0,
                                                     1, 0, 0,
                                                     0, 0, 1);

        public static Matrix OrientParedD = Helper.MatrixfromBasis(0, 1, 0,
                                                     -1, 0, 0,
                                                     0, 0, 1);



        public Block(Vector3 pos, GameModel pmodel, int ptipo , Matrix OrientBlock,int pcant_obstaculos)
        {
            model = pmodel;
            tipo = ptipo;
            Position = pos;
            Orient = ptipo ==0 ? Helper.CalcularUVN(pos) : Matrix.Identity;
            MatPos = Matrix.Translation(pos);
            matInvWorldBlock = matWorldBlock = OrientBlock * Orient* MatPos;
            matInvWorldBlock.Invert();
            cant_obstaculos = pcant_obstaculos;

            switch (ptipo)
            {
                default:
                    break;

                case 0:
                    Block0(OrientBlock);
                    break;

                case 1:
                    Block1(OrientBlock);
                    break;

                case 2:
                    Block2(OrientBlock);
                    break;

                case 3:
                    Block3(OrientBlock);
                    break;
            }
            // calculo el bounding box de toto el bloque
            Matrix T = Orient * MatPos;
            Vector3[] p = new Vector3[8];
            float min_x = 10000000, min_y = 10000000, min_z = 10000000;
            float max_x = -10000000, max_y = -10000000, max_z = -10000000;
            p[0] = new Vector3(-largo / 2, -alto / 2, -ancho / 2);
            p[1] = new Vector3(largo / 2, -alto / 2, -ancho / 2);
            p[2] = new Vector3(largo / 2, alto / 2, -ancho / 2);
            p[3] = new Vector3(-largo / 2, alto / 2, -ancho / 2);

            p[4] = new Vector3(-largo / 2, -alto / 2, ancho / 2);
            p[5] = new Vector3(largo / 2, -alto / 2, ancho / 2);
            p[6] = new Vector3(largo / 2, alto / 2, ancho / 2);
            p[7] = new Vector3(-largo / 2, alto / 2, ancho / 2);

            for (int i = 0; i < 8; ++i)
            {
                p[i].TransformCoordinate(T);
                if (p[i].X < min_x)
                    min_x = p[i].X;
                if (p[i].Y < min_y)
                    min_y = p[i].Y;
                if (p[i].Z < min_z)
                    min_z = p[i].Z;
                if (p[i].X > max_x)
                    max_x = p[i].X;
                if (p[i].Y > max_y)
                    max_y = p[i].Y;
                if (p[i].Z > max_z)
                    max_z = p[i].Z;
            }
            BoundingBox = new TgcBoundingAxisAlignBox(new Vector3(min_x, min_y, min_z), new Vector3(max_x, max_y, max_z));

            // matriz que transforma una caja de 1 x 1 x 1 ubicada en orgin y sin orientacion
            matWorldBock = Matrix.Scaling(new Vector3(largo, alto, ancho)) * T;

            // matriz que transforma una mesh list que constituye un block (de 100x100x100) ubicada en orgin y sin orientacion
            matWorldSurfaceBlock = Matrix.Translation(0, 7, 0) * Matrix.Scaling(new Vector3(0.01f, 0.1f, 0.01f)) * matWorldBock;


        }

        public int CreateObstaculo(Matrix OrientBlock,int t)
        {
            // obstaculos
            // nota: en el espacio del bloque 
            // X+ es hacia adelante , X- atras
            // Y+ es hacia arriba , Y- abajo
            // Z+ es hacia derecha, Z- izquierda

            Matrix S = Matrix.Scaling(k,k,k);

            for (int i = 0; i < cant_obstaculos; ++i)
            {
                int q = tipo_o++ % 6;
                if (random.Next(0, 100) > 25)
                    q = tipo_o++ % 6;
                if (random.Next(0, 100) > 25)
                    q = tipo_o++ % 6;

                float X = (-0.5f + 1.0f / (cant_obstaculos + 1) * (1 + i)) * largo;
                float xdesde = 0, xhasta = 0, ydesde = 0, yhasta = 0, zdesde = 0, zhasta = 0;
                xdesde = X - 2;
                xhasta = X + 2;

                switch (q)
                {
                    case 0:
                        // abajo
                        zdesde = -10; zhasta = 10; ydesde = -10; yhasta = 0.25f;
                        matWorld[t] = S * Matrix.Scaling(new Vector3(0.1f, 1, 2))
                            * Matrix.Translation(new Vector3(X, -5, 0)) * OrientBlock * Orient * MatPos;
                        break;
                    case 1:
                        // arriba
                        zdesde = -10; zhasta = 10; ydesde = -0.25f; yhasta = 10;
                        matWorld[t] = S * Matrix.Scaling(new Vector3(0.1f, 1, 2))
                            * Matrix.Translation(new Vector3(X, 5, 0)) * OrientBlock * Orient * MatPos;
                        break;
                    case 2:
                        // izquierda
                        zdesde = -10; zhasta = 0.25f; ydesde = -10; yhasta = 10;
                        matWorld[t] = S * Matrix.Scaling(new Vector3(0.1f, 2, 1))
                            * Matrix.Translation(new Vector3(X, 0, -5)) * OrientBlock * Orient * MatPos;
                        break;

                    case 3:
                        // derecha
                        zdesde = -0.25f; zhasta = 10; ydesde = -10; yhasta = 10;
                        matWorld[t] = S * Matrix.Scaling(new Vector3(0.1f, 2, 1))
                            * Matrix.Translation(new Vector3(X, 0, 5)) * OrientBlock * Orient * MatPos;
                        break;

                    case 4:
                        // izquierda y derecha
                        zdesde = -10; zhasta = -2.5f; ydesde = -10; yhasta = 10;
                        matWorld[t] = S * Matrix.Scaling(new Vector3(0.1f, 2, 0.6f))
                            * Matrix.Translation(new Vector3(X, 0, -6)) * OrientBlock * Orient * MatPos;
                        pmin[t] = new Vector3(xdesde, ydesde, zdesde);
                        pmax[t] = new Vector3(xhasta, yhasta, zhasta);
                        mesh_index[t++] = 6;

                        zdesde = 2.5f; zhasta = 10; ydesde = -10; yhasta = 10;
                        matWorld[t] = S * Matrix.Scaling(new Vector3(0.1f, 2, 0.6f))
                            * Matrix.Translation(new Vector3(X, 0, 6)) * OrientBlock * Orient * MatPos;
                        break;

                    case 5:
                        // arriba y abajo
                        zdesde = -10; zhasta = 10; ydesde = -10; yhasta = -2.5f;
                        matWorld[t] = S * Matrix.Scaling(new Vector3(0.1f, 0.6f, 2))
                            * Matrix.Translation(new Vector3(X, -6, 0)) * OrientBlock * Orient * MatPos;
                        pmin[t] = new Vector3(xdesde, ydesde, zdesde);
                        pmax[t] = new Vector3(xhasta, yhasta, zhasta);
                        mesh_index[t++] = 6;

                        zdesde = -10; zhasta = 10; ydesde = 2.5f; yhasta = 10;
                        matWorld[t] = S * Matrix.Scaling(new Vector3(0.1f, 0.6f, 2))
                            * Matrix.Translation(new Vector3(X, 6, 0)) * OrientBlock * Orient * MatPos;
                        break;


                }

                // almaceno el bounding box del obstaculo
                pmin[t] = new Vector3(xdesde, ydesde, zdesde);
                pmax[t] = new Vector3(xhasta, yhasta, zhasta);
                mesh_index[t++] = 6;
            }

            return t;
        }

        public void Block0(Matrix OrientBlock)
        {
            int prof = 2;       // profundidad del trench
            // piso y techo
            int t = 0;
            for (int i = -r; i < r; ++i)
            {
                //  piso
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), 5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), -5)) * OrientBlock * Orient * MatPos;
                ++t;


                // pared
                for (int s = 0; s < prof; ++s)
                {
                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, -10)) * OrientBlock * Orient * MatPos;
                    matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, 10)) * OrientBlock * Orient * MatPos;
                }
            }

            for (int i = 0; i < t; ++i)
                mesh_index[i] = random.Next(0, 5);

            cant_mesh = CreateObstaculo(OrientBlock, t);

        }


        public void Block1(Matrix OrientBlock)
        {
            int prof = 2;       // profundidad del trench
            // piso y techo
            int t = 0;
            for (int q = -r; q < r; ++q)
            {
                int i, i1, j, j1;
                if (q <= 0)
                {
                    i = i1 = q;
                    j = -1;
                    j1 = 0;
                }
                else
                {
                    j = j1 = q;
                    i = -1;
                    i1 = 0;
                }
                //  piso
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10 + 5, -10 * (prof - 1), j * 10 + 5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i1 * 10 + 5, -10 * (prof - 1), j1 * 10 + 5)) * OrientBlock * Orient * MatPos;
                ++t;

                //  techo
                matWorld[t] = Matrix.Scaling(escale) * OrientTecho * Matrix.Translation(new Vector3(i * 10 + 5, 10, j * 10 + 5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * OrientTecho * Matrix.Translation(new Vector3(i1 * 10 + 5, 10, j1 * 10 + 5)) * OrientBlock * Orient * MatPos;
                ++t;

                // pared
                for (int s = 0; s < prof; ++s)
                {
                    if (q <= 0)
                    {
                        matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(i * 10 + 5, 5 - 10 * s, j * 10)) * OrientBlock * Orient * MatPos;
                        if (q < -1)
                            matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(i1 * 10 + 5, 5 - 10 * s, j1 * 10 + 10)) * OrientBlock * Orient * MatPos;
                    }
                    else
                    {
                        matWorld[t++] = Matrix.Scaling(escale) * OrientParedU * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, j * 10 + 5)) * OrientBlock * Orient * MatPos;
                        matWorld[t++] = Matrix.Scaling(escale) * OrientParedD * Matrix.Translation(new Vector3(i1 * 10 + 10, 5 - 10 * s, j1 * 10 + 5)) * OrientBlock * Orient * MatPos;
                    }

                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedD * Matrix.Translation(new Vector3(10, 5 - 10 * s, -5)) * OrientBlock * Orient * MatPos;
                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedD * Matrix.Translation(new Vector3(10, 5 - 10 * s, 5)) * OrientBlock * Orient * MatPos;
                }
            }


            for (int i = 0; i < t; ++i)
                mesh_index[i] = random.Next(0, 5);

            cant_mesh = t;

        }

        public void Block2(Matrix OrientBlock)
        {
            int prof = 2;       // profundidad del trench
            // piso y techo
            int t = 0;
            for (int i = -r; i < r; ++i)
            {
                //  piso
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), 5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), -5)) * OrientBlock * Orient * MatPos;
                ++t;
                // pared
                for (int s = 0; s < prof; ++s)
                {
                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, -10)) * OrientBlock * Orient * MatPos;
                    matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, 10)) * OrientBlock * Orient * MatPos;
                }
            }


            for (int i = 0; i < t; ++i)
                mesh_index[i] = random.Next(0, 5);

            cant_mesh = CreateObstaculo(OrientBlock, t);
        }


        public void Block3(Matrix OrientBlock)
        {
            int prof = 2;       // profundidad del trench
            // piso y techo
            int t = 0;
            for (int i = -r; i < r; ++i)
            {
                //  piso
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10+5, -10 * (prof - 1), 5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10+5, -10 * (prof - 1), -5)) * OrientBlock * Orient * MatPos;
                ++t;


                
                //  techo
                matWorld[t] = Matrix.Scaling(escale) * OrientTecho * Matrix.Translation(new Vector3(i * 10+5, 10, 5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * OrientTecho * Matrix.Translation(new Vector3(i * 10+5, 10, -5)) * OrientBlock * Orient * MatPos;
                ++t;

                
                // pared
                if(i!=0 && i!=-1)
                for (int s = 0; s < prof; ++s)
                {
                   matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(i * 10+5, 5 - 10 * s, 10)) * OrientBlock * Orient * MatPos;
                   matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(i * 10+5, 5 - 10 * s, -10)) * OrientBlock * Orient * MatPos;
                }
            }

            for (int i = -r; i < r; ++i)
             if (i != 0 && i != -1)
             {
                //  piso
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(5, -10 * (prof - 1), i * 10+5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(-5, -10 * (prof - 1), i* 10+5)) * OrientBlock * Orient * MatPos;
                ++t;

                
                //  techo
                matWorld[t] = Matrix.Scaling(escale) * OrientTecho * Matrix.Translation(new Vector3(5, 10, i * 10+5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * OrientTecho * Matrix.Translation(new Vector3(-5, 10, i * 10+5)) * OrientBlock * Orient * MatPos;
                ++t;

                
                // pared
                for (int s = 0; s < prof; ++s)
                {
                   matWorld[t++] = Matrix.Scaling(escale) * OrientParedD * Matrix.Translation(new Vector3(10, 5 - 10 * s, i * 10+5)) * OrientBlock * Orient * MatPos;
                   matWorld[t++] = Matrix.Scaling(escale) * OrientParedU * Matrix.Translation(new Vector3(-10, 5 - 10 * s, i * 10+5)) * OrientBlock * Orient * MatPos;
                }
                
            }


            for (int i = 0; i < t; ++i)
                mesh_index[i] = random.Next(0, 5);

            cant_mesh = t;

        }



        public bool render()
        {
           // var r = TgcCollisionUtils.classifyFrustumAABB(model.Frustum, BoundingBox);
           // if (r == TgcCollisionUtils.FrustumResult.OUTSIDE)
           //     return false;
            
            float dist = (Position - model.Camara.Position).LengthSq();

            if (model.curr_mode == defines.MODO_GAME)
            {
                float dist_lod = 500000;
                if (dist > dist_lod)
                    return false;
            }


            // trench 
            for (int i = 0; i < cant_mesh; ++i)
            {
                int index = mesh_index[i];
                if (index != -1 )
                {
                    if (dist>20000 && index <= 5)
                        index += 7;
                    model.meshes[index].Transform = matWorld[i];
                    TgcShaders.Instance.setShaderMatrix(model.effect, matWorld[i]);
                    model.effect.SetValue("ssao", index==6?0:1);
                    model.effect.CommitChanges();
                    model.meshes[index].D3dMesh.DrawSubset(0);
                }
            }
            return true;
        }


        public bool colisiona(Vector3 pt)
        {
            // transformo el punto al espacio del block
            pt.TransformCoordinate(matInvWorldBlock);

            // choca contra las paredes
            if (pt.Z > 10 || pt.Z < -10)
                return true;

            // nota: en el espacio del bloque 
            // X+ es hacia adelante , X- atras
            // Y+ es hacia arriba , Y- abajo
            // Z+ es hacia derecha, Z- izquierda

            bool rta = false;
            for (int i = 0; i < cant_mesh && !rta; ++i)
            {
                if(mesh_index[i]==6)
                {
                    // obstaculo
                    if (pt.Z >= 0)
                    {
                        int bp = 1;
                    }

                    if (pt.Z >= pmin[i].Z && pt.Z <= pmax[i].Z && 
                        pt.Y >= pmin[i].Y && pt.Y <= pmax[i].Y &&
                        pt.X >= pmin[i].X && pt.X <= pmax[i].X)
                        rta = true;
                }
            }
            return rta;
        }



    }


    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer m�s ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {
        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
        }


        //public int curr_mode = defines.MODO_CAMARA;
        //public int curr_mode = defines.MODO_TEST_BLOCK;
        public int curr_mode = defines.MODO_GAME;
        public bool camara_ready = false;

        public TgcBox Box { get; set; }
        public List<TgcMesh> meshes = new List<TgcMesh>();
        public List<TgcMesh> xwing = new List<TgcMesh>();
        public TgcArrow ship = new TgcArrow();
        public TgcBox BlockSurface;
        public TgcBox BlockTrench;
        public TgcBox LODMesh;

        public float star_r = 8000;
        public float ship_k = 0.06f;
        public Vector3 ship_vel;
        public Vector3 ship_N;
        public Vector3 ship_bitan;
        public float ship_speed;
        public Vector3 ship_pos;
        public float ship_an = 0;
        public float ship_an_base = 0;
        public float ship_anV = 0;
        public float ship_H;
        public Vector3 cam_vel = new Vector3(0, 0, 0);
        public Vector3 cam_pos = new Vector3(1, 0, 0);
        public Vector3 target_pos = new Vector3(1, 0, 0);
        public List<Block> scene = new List<Block>();
        public List<Block> trench = new List<Block>();
        int curr_block;
        public Vector3[] collision_pt = new Vector3[5];
        public int cant_cpt = 5;        // cantidad de puntos de colision
        public int cd_index = 0;        // colision detectada
        public float explosion_timer = 0;      // timer colision detectada
        public float tiempo_explosion = 2;      // tiempo total que tarda en explotar
        public float intro_timer = 10;   // timer de introduccion
        public float r_timer = 0;      // timer de resurreccion
        public Vector4 _Sphere;
        

        private static readonly Random random = new Random();

        public Effect effect;
        public Surface g_pDepthStencil; // Depth-stencil buffer
        public Texture g_pRenderTarget, g_pPosition, g_pNormal;
        public VertexBuffer g_pVBV3D;
        public Texture textura_bloques;

        public float time = 0;


        public bool mouseCaptured;
        public Point mouseCenter;
        public float xm, ym;     // pos del mouse
        public float wm;
        public int eventoInterno = 0;


        public override void Init()
        {
            // cargo el shader
            InitDefferedShading();
            // la iluminacion
            InitLighting();
            // cargo la escena
            InitScene();
            // pos. la camara
            InitCamara();
            // el input del mouse
            // para capturar el mouse
            var focusWindows = D3DDevice.Instance.Device.CreationParameters.FocusWindow;
            mouseCenter = focusWindows.PointToScreen(new Point(focusWindows.Width / 2, focusWindows.Height / 2));
            mouseCaptured = false;
            Cursor.Position = mouseCenter;
            //            Cursor.Hide();


            xm = Input.Xpos;
            ym = Input.Ypos;
            wm = Input.WheelPos;
        }


        public void InitScene()
        {
            //Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;
            var textura_surface = TgcTexture.createTexture(MediaDir + "Textures\\ds_surface.png");
            effect.SetValue("texDeathStarSurface", textura_surface.D3dTexture);
            Box = TgcBox.fromSize(new Vector3(1, 1, 1), textura_surface);
            Box.Position = new Vector3(0, 0, 0);
            Box.Effect = effect;
            Box.Technique = "DefaultTechnique";
            BlockSurface = TgcBox.fromSize(new Vector3(1, 1, 1), textura_surface);
            BlockSurface.Effect = effect;
            BlockSurface.Technique = "DefaultTechnique";
            BlockTrench = TgcBox.fromSize(new Vector3(1, 1, 1), TgcTexture.createTexture(MediaDir + "Textures\\ds_trench.png"));
            BlockTrench.Effect = effect;
            BlockTrench.Technique = "DefaultTechnique";

            var loader = new TgcSceneLoader();
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m1-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m2-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m3-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m4-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m5-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "torreta-TgcScene.xml").Meshes[0]);       // 5
            meshes.Add(TgcBox.fromSize(new Vector3(100, 100, 100), textura_surface).toMesh("q2"));
            //meshes.Add(loader.loadSceneFromFile(MediaDir + "q2-TgcScene.xml").Meshes[0]);          // 6
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x1-TgcScene.xml").Meshes[0]);               // 7
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x2-TgcScene.xml").Meshes[0]);               // 8
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x3-TgcScene.xml").Meshes[0]);               // 9
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x4-TgcScene.xml").Meshes[0]);               // 10
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x5-TgcScene.xml").Meshes[0]);               // 11
            meshes.Add(loader.loadSceneFromFile(MediaDir + "torreta2-TgcScene.xml").Meshes[0]);          // 12

            LODMesh = TgcBox.fromSize(meshes[0].BoundingBox.calculateSize(),
                        TgcTexture.createTexture(MediaDir + "Textures\\m1.jpg"));
            LODMesh.Effect = effect;
            LODMesh.Technique = "DefaultTechnique";

            foreach (TgcMesh mesh in meshes)
            {
                mesh.Scale = new Vector3(1f, 1f, 1f);
                mesh.AutoTransformEnable = false;
                mesh.Effect = effect;
                mesh.Technique = "DefaultTechnique";
            }

            xwing = loader.loadSceneFromFile(MediaDir + "xwing-TgcScene.xml").Meshes;
            foreach (TgcMesh mesh in xwing)
            {
                mesh.AutoTransformEnable = false;
                mesh.Effect = effect;
                mesh.Technique = "DefaultTechnique";
            }

            if (curr_mode == defines.MODO_TEST_BLOCK)
            {
                scene.Add(new Block(new Vector3(0,0,0), this, 2, Matrix.Identity,1));
            }
            else
            {
                ArmarEcuatorialTrench();
            }



            var textura_skybox = TgcTexture.createTexture(MediaDir + "Textures\\Color A05.png");
            effect.SetValue("texSkybox", textura_skybox.D3dTexture);
            textura_bloques = TgcTexture.createTexture(MediaDir + "Textures\\4.png").D3dTexture;

            // puntos de colision de la nave
            collision_pt[0] = new Vector3(0, -10, -50);          // ala izquierda
            collision_pt[1] = new Vector3(0, 10, -50);          // ala izquierda
            collision_pt[2] = new Vector3(0, -10, 50);            // ala derecha
            collision_pt[3] = new Vector3(0, 10, 50);            // ala derecha
            collision_pt[4] = new Vector3(50, 0, 0);          // frente



        }

        public void InitCamara()
        {
            Vector3 cameraPosition;
            Vector3 lookAt;
            if (curr_mode == defines.MODO_GAME)
            {
                //ship_pos = new Vector3(0, 50, star_r-15);
                ship_pos = scene[scene.Count-1].Position;
                ship_vel = new Vector3(0, -1, 0);
                ship_N = new Vector3(0, 0, 1);
                ship_bitan = new Vector3(-1, 0, 0);
                ship_speed = 250;            // despues de la intro la subo a 250
            }
            else
            {
                if (curr_mode == defines.MODO_TEST_BLOCK)
                {
                    Vector3 pmin = new Vector3(10000, 10000, 10000);
                    Vector3 pmax = new Vector3(-10000, -10000, -10000);
                    foreach (Block bloque in scene)
                    {
                        if (bloque.BoundingBox.PMin.X < pmin.X)
                            pmin.X = bloque.BoundingBox.PMin.X;
                        if (bloque.BoundingBox.PMin.Y < pmin.Y)
                            pmin.Y = bloque.BoundingBox.PMin.Y;
                        if (bloque.BoundingBox.PMin.Z < pmin.Z)
                            pmin.Z = bloque.BoundingBox.PMin.Z;

                        if (bloque.BoundingBox.PMax.X > pmax.X)
                            pmax.X = bloque.BoundingBox.PMax.X;
                        if (bloque.BoundingBox.PMax.Y > pmax.Y)
                            pmax.Y = bloque.BoundingBox.PMax.Y;
                        if (bloque.BoundingBox.PMax.Z > pmax.Z)
                            pmax.Z = bloque.BoundingBox.PMax.Z;
                    }

                    lookAt = (pmin + pmax)*0.5f;
                    cameraPosition = lookAt + new Vector3(1000,500,0);
                }
                else
                {
                    cameraPosition = new Vector3(1000, 2000, 0);
                    lookAt = new Vector3(0, 0, 0);


                    // test de explosion
                    cameraPosition = new Vector3(-4.844278f, -200.8399f, 7989.138f);
                    lookAt = new Vector3(-7.664487f, -210.4348f, 7988.982f);
                    _Sphere = new Vector4(-10.02304f, -209.638f, 7986.195f, 1.0f);

                    /*
                    cameraPosition = new Vector3(20, 0,0);
                    lookAt = new Vector3(0,0,0);
                    _Sphere = new Vector4(0, 0, 0, 1.0f);
                    */

                }
                Camara.SetCamera(cameraPosition, lookAt , new Vector3(0,1,0));
            }

        }



        public void InitDefferedShading()
        {
            var d3dDevice = D3DDevice.Instance.Device;
            //Cargar Shader personalizado
            string compilationErrors;
            effect = Effect.FromFile(d3dDevice, ShadersDir + "starwars.fxo", null, null, ShaderFlags.PreferFlowControl,
                null, out compilationErrors);
            if (effect == null)
            {
                throw new Exception("Error al cargar shader. Errores: " + compilationErrors);
            }
            effect.Technique = "DefaultTechnique";

            g_pDepthStencil = d3dDevice.CreateDepthStencilSurface(d3dDevice.PresentationParameters.BackBufferWidth,
                d3dDevice.PresentationParameters.BackBufferHeight,
                DepthFormat.D24S8, MultiSampleType.None, 0, true);

            // inicializo el render target
            g_pRenderTarget = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8,
                Pool.Default);
            // geometry buffer
            g_pNormal = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.A32B32G32R32F,
                Pool.Default);
            // geometry buffer
            g_pPosition = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.A32B32G32R32F,
                Pool.Default);

            effect.SetValue("g_RenderTarget", g_pRenderTarget);
            // Resolucion de pantalla
            effect.SetValue("screen_dx", d3dDevice.PresentationParameters.BackBufferWidth);
            effect.SetValue("screen_dy", d3dDevice.PresentationParameters.BackBufferHeight);

            CustomVertex.PositionTextured[] vertices =
            {
            new CustomVertex.PositionTextured(-1, 1, 1, 0, 0),
            new CustomVertex.PositionTextured(1, 1, 1, 1, 0),
            new CustomVertex.PositionTextured(-1, -1, 1, 0, 1),
            new CustomVertex.PositionTextured(1, -1, 1, 1, 1)
        };
            //vertex buffer de los triangulos
            g_pVBV3D = new VertexBuffer(typeof(CustomVertex.PositionTextured),
                4, d3dDevice, Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionTextured.Format, Pool.Default);
            g_pVBV3D.SetData(vertices, 0, LockFlags.None);


            var textura_ruido = TgcTexture.createTexture(MediaDir + "Textures\\noise.png");
            effect.SetValue("texNoise", textura_ruido.D3dTexture);


        }

        public void InitLighting()
        {
            Vector3 dir = new Vector3(0, -600, 300);
            dir.Normalize();
            Vector3 LP = curr_mode == defines.MODO_TEST_BLOCK ? new Vector3(0, 150, 0) : dir * 10000;
            Vector3 LightDir = LP;
            LightDir.Normalize();
            //Cargar variables shader de la luz
            effect.SetValue("lightColor", ColorValue.FromColor(Color.FromArgb(240, 240, 255)));
            effect.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(LP));
            effect.SetValue("lightDir", TgcParserUtils.vector3ToFloat4Array(LightDir));
            effect.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
            effect.SetValue("lightIntensity", (float)1);
            effect.SetValue("lightAttenuation", (float)0);

            //Cargar variables de shader de Material. El Material en realidad deberia ser propio de cada mesh. Pero en este ejemplo se simplifica con uno comun para todos
            effect.SetValue("materialEmissiveColor", ColorValue.FromColor(Color.FromArgb(0, 0, 0)));
            effect.SetValue("materialAmbientColor", ColorValue.FromColor(Color.FromArgb(120, 120, 120)));
            effect.SetValue("materialDiffuseColor", ColorValue.FromColor(Color.FromArgb(120, 120, 120)));
            effect.SetValue("materialSpecularColor", ColorValue.FromColor(Color.FromArgb(240, 204, 155)));
            effect.SetValue("materialSpecularExp", (float)40);
            effect.SetValue("specularFactor", (float)1.3);
        }


        public void ArmarEcuatorialTrench()
        {
            float alfa_0 = 0;
            float alfa_1 = (float)(2*Math.PI);
            float cant_i = (int)(star_r * (alfa_1 - alfa_0) / (Block.largo * 0.8f));
            for (int i = 0; i < cant_i; ++i)
            {
                float ti = i / cant_i;
                float alfa = alfa_0 * (1 - ti) + alfa_1 * ti;
                float x = FastMath.Cos(alfa);
                float y = FastMath.Sin(alfa);
                float j = cant_i - i-1;
                scene.Add(new Block(new Vector3(0, y, x) * (star_r-10), this, 0 , Matrix.Identity, 
                         j<5? 0 :  1 + (int)Math.Floor(j/cant_i * 5)));
            }
        }




        public override void Update()
        {
            PreUpdate();
            if (ElapsedTime < 0 || ElapsedTime > 10)
                return;

            // timers
            time += ElapsedTime;
            if (intro_timer > 0)
            {
                ship_speed = 50;
                intro_timer -= ElapsedTime;
                if (intro_timer < 0)
                {
                    intro_timer = 0;
                    ship_speed = 250;
                }
            }

            if (explosion_timer>0)
            {
                ship_speed = 5;
                explosion_timer -= ElapsedTime;
                if (explosion_timer < 0)
                {
                    explosion_timer = 0;
                    ship_speed = 250;
                    // crash
                    // ship_pos = new Vector3(0, 50, star_r - 15);
                    //ship_pos = scene[scene.Count - 1].Position;
                    ship_pos = scene[curr_block].Position;
                    ship_vel = new Vector3(0, -1, 0);
                    ship_N = new Vector3(0, 0, 1);
                    ship_bitan = new Vector3(-1, 0, 0);
                    ship_an_base = ship_an = 0;
                    ship_anV = 0;
                    // le doy 2 segundos de changui
                    r_timer = 2;
                }
            }

            if (r_timer > 0)
            {
                r_timer -= ElapsedTime;
                if (r_timer < 0)
                    r_timer = 0;
            }

            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftControl))
                ship_an_base = (float)Math.PI / 2.0f;
            else
                ship_an_base = 0;

            if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.M))
            {
                mouseCaptured = !mouseCaptured;
                if (mouseCaptured)
                    Cursor.Hide();
                else
                    Cursor.Show();
            }

            if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_RIGHT))
                ElapsedTime = 0;

            if (Input.buttonUp(TgcD3dInput.MouseButtons.BUTTON_LEFT) || Input.buttonUp(TgcD3dInput.MouseButtons.BUTTON_MIDDLE))
            {
                eventoInterno = 0;
            }

            if (mouseCaptured || Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                if (eventoInterno == 0)
                {
                    xm = Input.Xpos;
                    ym = Input.Ypos;
                    eventoInterno = 1;
                }
                else
                {
                    float dx = Input.XposRelative * 10;
                    float dy = Input.YposRelative * 10;

                    if (curr_mode == defines.MODO_GAME)
                    {
                        if (!Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift))
                        {
                            dx *= 0.3f;
                            dy *= 0.5f;
                        }

                       
                        Matrix rotN = Matrix.RotationAxis(ship_N, ElapsedTime * dx);
                        ship_vel.TransformNormal(rotN);
                        ship_bitan.TransformNormal(rotN);
                        ship_an += ElapsedTime * dx * 5.0f;
                        ship_an = FastMath.Clamp(ship_an, -1, 1);
                        ship_anV += ElapsedTime * dy * 5.0f;
                        ship_anV = FastMath.Clamp(ship_anV, -1, 1);

                        Matrix rotBT = Matrix.RotationAxis(ship_bitan, -ElapsedTime * dy);
                        ship_vel.TransformNormal(rotBT);
                        ship_N.TransformNormal(rotBT);

                    }
                    else
                    {
                        // uso el desplazamiento en x para rotar el punto de vista 
                        // en el plano xy
                        float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift) ? 0.05f : 0.5f;
                        float tot_x = 800;
                        float an = dx / tot_x * 2 * FastMath.PI * k;
                        Matrix T = Matrix.Translation(-Camara.LookAt) * Matrix.RotationY(an) * Matrix.Translation(Camara.LookAt);
                        Vector3 LF = Camara.Position;
                        LF.TransformCoordinate(T);

                        Vector3 ViewDir = Camara.LookAt - Camara.Position;
                        ViewDir.Normalize();

                        Vector3 N;
                        N = Vector3.Cross(new Vector3(0, 1, 0), ViewDir);

                        float tot_y = 600;
                        float an_y = dy / tot_y * FastMath.PI * k;
                        LF = Helper.rotar(LF, Camara.LookAt, N, an_y);

                        Camara.SetCamera(LF, Camara.LookAt);


                    }
                }
            }

            if (mouseCaptured)
                Cursor.Position = mouseCenter;


            if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_MIDDLE))
            {
                if (eventoInterno == 0)
                {
                    xm = Input.Xpos;
                    ym = Input.Ypos;
                    eventoInterno = 1;
                }
                else
                {
                    float dx = Input.Xpos - xm;
                    float dy = Input.Ypos - ym;
                    xm = Input.Xpos;
                    ym = Input.Ypos;

                    float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftControl) ? 0.5f : 1f;

                    Vector3 VUP = new Vector3(0, 1, 0);
                    Vector3 d = Camara.LookAt - Camara.Position;
                    float dist = d.Length();
                    // mido la pantalla en el plano donde esta el LookAt
                    float fov = FastMath.QUARTER_PI;
                    float aspect = 1;
                    float Width = 1200;
                    float Height = 900;
                    float kx = 2 * FastMath.Tan(fov / 2) * dist * aspect / Width * k;
                    float ky = 2 * FastMath.Tan(fov / 2) * dist / Height * k;
                    d.Normalize();
                    Vector3 n = Vector3.Cross(d, VUP);
                    n.Normalize();
                    Vector3 up = Vector3.Cross(n, d);
                    Vector3 desf = up * (dy * ky) + n * (dx * kx);
                    Camara.SetCamera(Camara.Position + desf, Camara.LookAt + desf);
                }
            }

            float zDelta = Input.WheelPos;
            wm = Input.WheelPos;
            if (FastMath.Abs(zDelta) > 0.1f)
            {
                float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift) ? 10 : 100;
                Vector3 LF = Camara.Position;
                Vector3 ViewDir = Camara.LookAt - LF;
                ViewDir.Normalize();

                Vector3 LA = Camara.LookAt;
                float dist = (LA - LF).Length();
                if (zDelta > 0)
                {
                    LF = LF + ViewDir * k;
                }
                else
                {
                    LF = LF - ViewDir * k;
                }
                Camara.SetCamera(LF, Camara.LookAt);
            }



            if (curr_mode == defines.MODO_GAME)
            {
                float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift) ? 0.01f : 0.4f;
                Vector3 ant_ship_pos = ship_pos;
                ship_pos = ship_pos + ship_vel * ElapsedTime * ship_speed * k;
                ship_H = ship_pos.Length() - star_r;

                // chequeo que no choque contra el piso y/o el techo
                float min_H = -17;
                float max_H = -5;
                if (ship_H < min_H || ship_H>max_H)
                {
                    Vector3 N  = ship_pos;
                    N.Normalize();
                    if (ship_H < min_H )
                        ship_pos = N * (star_r + min_H);
                    else
                        ship_pos = N * (star_r + max_H);
                    // lo roto un poco para arriba / abajo para que no siga chocando
                    Matrix rotBT = Matrix.RotationAxis(ship_bitan, ship_H < min_H ? 0.001f : -0.001f);
                    ship_vel.TransformNormal(rotBT);
                    ship_N.TransformNormal(rotBT);
                }

                // determino en que bloque estoy:
                curr_block = 0;
                float min_dist = 100000000;
                for (int i = 0; i < scene.Count; ++i)
                {
                    float dist = (scene[i].Position - ship_pos).LengthSq();
                    if (dist < min_dist)
                    {
                        curr_block = i;
                        min_dist = dist;
                    }
                }


                // colision con obstaculos
                if (r_timer == 0 && explosion_timer==0)
                {
                    Matrix O = Helper.MatrixfromBasis(
                                                        1, 0, 0,
                                                        0, 0, 1,
                                                        0, 1, 0
                                                        );
                    if (Math.Abs(ship_an + ship_an_base) > 0.001f)
                        O = O * Matrix.RotationX(ship_an_base + ship_an);
                    if (Math.Abs(ship_anV) > 0.001f)
                        O = O * Matrix.RotationY(ship_anV);

                    Matrix T = O * Helper.CalcularMatriz(ship_pos, new Vector3(ship_k, ship_k, ship_k), ship_vel, ship_bitan, ship_N);
                    Vector3[] p = new Vector3[cant_cpt];
                    for (int s = 0; s < cant_cpt; ++s)
                    {
                        p[s] = new Vector3(0, 0, 0);
                        p[s].TransformCoordinate(Matrix.Translation(collision_pt[s]) * T);
                    }

                    bool colisiona = false;
                    for (int s = 0; s < cant_cpt && !colisiona; ++s)
                    {
                        if (scene[curr_block].colisiona(p[s]))
                        {
                            colisiona = true;
                            explosion_timer = tiempo_explosion;
                            cd_index = s;
                        }
                    }
                }

                if (ship_an != 0)
                {
                    ship_an -= ElapsedTime * 1.5f * Math.Sign(ship_an);
                    if (FastMath.Abs(ship_an) < 0.01f)
                        ship_an = 0;
                }
                if (ship_anV != 0)
                {
                    ship_anV -= ElapsedTime * 1.5f * Math.Sign(ship_anV);
                    if (FastMath.Abs(ship_anV) < 0.01f)
                        ship_anV = 0;
                }
                Vector3 ViewDir = Camara.LookAt - Camara.Position;
                float cam_dist = ViewDir.Length();
                ViewDir.Normalize();
                Vector3 desired_LF = ship_pos - ship_vel * 10 + ship_N * 0.8f;
                Vector3 desired_LA = ship_pos + ship_N * 1.0f;
                Vector3 cam_N = ship_pos;
                cam_N.Normalize();
                Camara.SetCamera(desired_LF, desired_LA, cam_N);
                UpdateView();

            }

        }


        
        public override void Render()
        {
            var device = D3DDevice.Instance.Device;
            // lighting pass 
            var pOldRT = device.GetRenderTarget(0);
            var pOldDS = device.DepthStencilSurface;

            var pSurf = g_pRenderTarget.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            device.DepthStencilSurface = g_pDepthStencil;
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();

            if (curr_mode == defines.MODO_GAME)
            {
                // dibujo el quad pp dicho :
                if (explosion_timer > 0)
                {
                    float t = (tiempo_explosion - explosion_timer) / tiempo_explosion;
                    effect.Technique = "Explosion";
                    Matrix O = Helper.MatrixfromBasis(
                                            1, 0, 0,
                                            0, 0, 1,
                                            0, 1, 0
                                            );
                    if (Math.Abs(ship_an + ship_an_base) > 0.001f)
                        O = O * Matrix.RotationX(ship_an_base + ship_an);
                    if (Math.Abs(ship_anV) > 0.001f)
                        O = O * Matrix.RotationY(ship_anV);
                    Matrix T = O * Helper.CalcularMatriz(ship_pos, new Vector3(ship_k, ship_k, ship_k), ship_vel, ship_bitan, ship_N);
                    Vector3 pt = new Vector3(0, 0, 0);
                    pt.TransformCoordinate(Matrix.Translation(collision_pt[cd_index]) * T);
                    effect.SetValue("_Sphere", new Vector4(pt.X, pt.Y, pt.Z, 10.0f*t + 0.5f));
                    effect.SetValue("_NoiseAmp", -10f * t);
                    effect.SetValue("_NoiseFreq", 1.1f);

                }
                else
                    effect.Technique = "SkyBox";

                device.VertexFormat = CustomVertex.PositionTextured.Format;
                device.SetStreamSource(0, g_pVBV3D, 0);
                Vector3 ViewDir = Camara.LookAt - Camara.Position;
                ViewDir.Normalize();

                Vector3 Up = curr_mode == defines.MODO_CAMARA ? new Vector3(0, 1, 0) : ship_N;
                Vector3 U, V;
                V = Vector3.Cross(ViewDir, Up);
                V.Normalize();
                U = Vector3.Cross(V, ViewDir);
                U.Normalize();

                float fov = D3DDevice.Instance.FieldOfView;
                float W = device.PresentationParameters.BackBufferWidth;
                float H = device.PresentationParameters.BackBufferHeight;
                float k = 2 * FastMath.Tan(fov / 2) / H;
                Vector3 Dy = U * k;
                Vector3 Dx = V * k;
                float Zn = D3DDevice.Instance.ZNearPlaneDistance;
                float Zf = D3DDevice.Instance.ZFarPlaneDistance;
                float Q = Zf / (Zf - Zn);

                effect.SetValue("LookFrom", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
                effect.SetValue("ViewDir", TgcParserUtils.vector3ToFloat4Array(ViewDir));
                effect.SetValue("Dx", TgcParserUtils.vector3ToFloat4Array(Dx));
                effect.SetValue("Dy", TgcParserUtils.vector3ToFloat4Array(Dy));
                effect.SetValue("MatProjQ", Q);
                effect.SetValue("Zn", Zn);
                effect.SetValue("Zf", Zf);

                effect.Begin(FX.None);
                effect.BeginPass(0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                effect.EndPass();
                effect.End();
            }
            else
            {
                // TEST explosion
                effect.Technique = "Explosion";
                device.VertexFormat = CustomVertex.PositionTextured.Format;
                device.SetStreamSource(0, g_pVBV3D, 0);
                Vector3 ViewDir = Camara.LookAt - Camara.Position;
                ViewDir.Normalize();

                Vector3 Up = curr_mode == defines.MODO_CAMARA ? new Vector3(0, 1, 0) : ship_N;
                Vector3 U, V;
                V = Vector3.Cross(ViewDir, Up);
                V.Normalize();
                U = Vector3.Cross(V, ViewDir);
                U.Normalize();

                float fov = D3DDevice.Instance.FieldOfView;
                float W = device.PresentationParameters.BackBufferWidth;
                float H = device.PresentationParameters.BackBufferHeight;
                float k = 2 * FastMath.Tan(fov / 2) / H;
                Vector3 Dy = U * k;
                Vector3 Dx = V * k;
                float Zn = D3DDevice.Instance.ZNearPlaneDistance;
                float Zf = D3DDevice.Instance.ZFarPlaneDistance;
                float Q = Zf / (Zf - Zn);

                effect.SetValue("LookFrom", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
                effect.SetValue("ViewDir", TgcParserUtils.vector3ToFloat4Array(ViewDir));
                effect.SetValue("Dx", TgcParserUtils.vector3ToFloat4Array(Dx));
                effect.SetValue("Dy", TgcParserUtils.vector3ToFloat4Array(Dy));
                effect.SetValue("MatProjQ", Q); 
                effect.SetValue("Zn", Zn);
                effect.SetValue("Zf", Zf);
                effect.SetValue("time", time);

                Vector3 pt = new Vector3(0,0,0);
                effect.SetValue("_Sphere", _Sphere + new Vector4(0,0,0,(float)Math.Sin(time) + 0.5f));
                effect.SetValue("_NoiseAmp", -5f);
                effect.SetValue("_NoiseFreq", 1.1f);

                effect.Begin(FX.None);
                effect.BeginPass(0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                effect.EndPass();
                effect.End();

            }

            RenderScene("DefaultTechnique");
            device.EndScene();
            pSurf.Dispose();


            device.DepthStencilSurface = pOldDS;
            device.SetRenderTarget(0, pOldRT);

            // dibujo el quad pp dicho :
            device.BeginScene();
            effect.Technique = "PostProcess";
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.SetStreamSource(0, g_pVBV3D, 0);
            effect.SetValue("g_RenderTarget", g_pRenderTarget);
            effect.SetValue("g_Position", g_pPosition);
            effect.SetValue("g_Normal", g_pNormal);
            effect.SetValue("matProj", device.Transform.Projection);

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            effect.Begin(FX.None);
            effect.BeginPass(0);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            effect.EndPass();
            effect.End();
            device.EndScene();


            device.BeginScene();

            /*
            switch (curr_mode)
            {
                case defines.MODO_TEST_BLOCK:
                    DrawText.drawText("TEST BLOCK", 700, 30, Color.Yellow);
                    break;
                case defines.MODO_GAME:
                    DrawText.drawText("ship_pos: " + (int)ship_pos.X + "," + (int)ship_pos.Y + "," + (int)ship_pos.Z, 5, 20, Color.Yellow);
                    DrawText.drawText("ship_spped: " + ship_speed, 5, 40, Color.Yellow);
                    DrawText.drawText("GAME MODE    (shift accel)", 700, 30, Color.Yellow);
                    DrawText.drawText("Curr block #" +curr_block,400, 10, Color.Yellow);
                    break;
                case defines.MODO_CAMARA:
                default:
                    DrawText.drawText("CAm pos: " + Camara.Position.X + "," + Camara.Position.Y + "," + Camara.Position.Z, 5, 20, Color.Yellow);
                    break;
            }
            */

            if (curr_mode == defines.MODO_GAME)
            {
                DrawText.drawText("Bloques Completados: " + (int)(scene.Count - curr_block - 1), 400, 10, Color.Yellow);
                if (intro_timer > 0)
                {
                    if (intro_timer > 3f)
                    {
                        DrawText.drawText("MOUSE   --> mover ", 400, 100, Color.Yellow);
                        DrawText.drawText("CONTROL --> girar 90 grados", 400, 140, Color.Yellow);
                        DrawText.drawText("M       --> lock / unlock mouse  ", 400, 180, Color.Yellow);
                        DrawText.drawText("Esquiva obstaculos y paredes", 400, 400, Color.Yellow);
                        DrawText.drawText("El objetivo es llegar al final de trench", 400, 420, Color.Yellow);
                    }
                    else
                        DrawText.drawText((int)intro_timer + "s para comenzar...", 400, 320, Color.Yellow);
                }
            }

            // debug colision
            /*
            // transformo el punto al espacio del block
            Vector3 pt = ship_pos;
            pt.TransformCoordinate(scene[curr_block].matInvWorldBlock);

            {
                Matrix O = Helper.MatrixfromBasis(
                                        1, 0, 0,
                                        0, 0, 1,
                                        0, 1, 0
                                        );
                if (Math.Abs(ship_an + ship_an_base) > 0.001f)
                    O = O * Matrix.RotationX(ship_an_base + ship_an);
                if (Math.Abs(ship_anV) > 0.001f)
                    O = O * Matrix.RotationY(ship_anV);
                Matrix T = O * Helper.CalcularMatriz(ship_pos, new Vector3(ship_k, ship_k, ship_k), ship_vel, ship_bitan, ship_N);
                Vector3 p = new Vector3(0, 0, 0);
                p.TransformCoordinate(Matrix.Translation(collision_pt[0]) * T);
                p.TransformCoordinate(scene[curr_block].matInvWorldBlock);
                DrawText.drawText(      "X=" + (int)Math.Round(p.X,0) + 
                                        "   Y=" + (int)Math.Round(p.Y,0) + 
                                        "   Z=" + (int)Math.Round(p.Z,0), 400, 110, Color.Yellow);
            }
            */

            RenderFPS();
            device.EndScene();
            device.Present();
        }

        public void RenderBloques()
        {
            effect.SetValue("texDiffuseMap", textura_bloques);
            effect.SetValue("ssao", 1);
            effect.Begin(0);
            effect.BeginPass(0);
            foreach (Block bloque in scene)
            {
                bloque.render();
            }
            effect.EndPass();
            effect.End();
        }


        public void RenderScene(String technique)
        {
            var device = D3DDevice.Instance.Device;
            effect.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
            effect.Technique = technique;

            if (curr_mode != defines.MODO_CAMARA)
                RenderBloques();

            bool render_ship = curr_mode == defines.MODO_GAME ? true : false;
            if (render_ship && r_timer != 0)
                render_ship = (int)(r_timer * 1000) % 2 == 0 ? true : false;

            if (render_ship)
            {
                // render ship
                Matrix O = Helper.MatrixfromBasis(
                                        1, 0, 0,
                                        0, 0, 1,
                                        0, 1, 0
                                        );
                if (Math.Abs(ship_an + ship_an_base)> 0.001f )
                    O = O * Matrix.RotationX(ship_an_base + ship_an);
                if (Math.Abs(ship_anV) > 0.001f)
                    O = O * Matrix.RotationY(ship_anV);

                Matrix T = O * Helper.CalcularMatriz(ship_pos, new Vector3(ship_k, ship_k, ship_k), ship_vel, ship_bitan, ship_N);
                effect.SetValue("ssao", 0);
                foreach (TgcMesh mesh in xwing)
                {
                    mesh.Transform = T;
                    mesh.Technique = technique;
                    mesh.render();
                }


                // debug colision
                // 
                /*
                if (cd_timer > 0 )
                {
                    Box.Technique = technique;
                    Box.Transform = Matrix.Scaling(new Vector3(1, 1, 1)*(1-cd_timer)*30) * Matrix.Translation(collision_pt[cd_index]) * T;
                    Box.render();
                }*/
            }



        }

        /// <summary>
        ///     Se llama cuando termina la ejecuci�n del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gr�ficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            Box.dispose();
            foreach (TgcMesh mesh in meshes)
                mesh.dispose();
        }
    }

    public class Helper
    {
        // helpers varios
        static public Matrix CalcularMatriz(Vector3 Pos, Vector3 Scale, Vector3 Dir)
        {
            // determino la orientacion
            Vector3 U = Vector3.Cross(new Vector3(0, 1, 0), Dir);
            U.Normalize();
            if (FastMath.Abs(U.X) < 0.001f && FastMath.Abs(U.Y) < 0.001f && FastMath.Abs(U.Z) < 0.001f)
                U = Vector3.Cross(new Vector3(0, 0, 1), Dir);
            Vector3 V = Vector3.Cross(Dir, U);
            Matrix matWorld = Matrix.Scaling(Scale);
            Matrix Orientacion;
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = Dir.X;
            Orientacion.M32 = Dir.Y;
            Orientacion.M33 = Dir.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * Matrix.Translation(Pos);
            return matWorld;
        }


        static public Matrix CalcularMatriz(Vector3 Pos, Vector3 Scale, Vector3 U, Vector3 V, Vector3 N)
        {
            Matrix matWorld = Matrix.Scaling(Scale);
            Matrix Orientacion;
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = N.X;
            Orientacion.M32 = N.Y;
            Orientacion.M33 = N.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * Matrix.Translation(Pos);
            return matWorld;
        }



        static public Matrix CalcularUVN(Vector3 Dir)
        {
            // determino la orientacion
            Dir.Normalize();
            Vector3 U = Vector3.Cross(new Vector3(1, 0, 0), Dir);
            if (FastMath.Abs(U.X) < 0.001f && FastMath.Abs(U.Y) < 0.001f && FastMath.Abs(U.Z) < 0.001f)
                U = Vector3.Cross(new Vector3(0, 1, 0), Dir);
            U.Normalize();
            Vector3 V = Vector3.Cross(Dir, U);
            V.Normalize();
            Matrix Orientacion;
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M31 = V.X;
            Orientacion.M32 = V.Y;
            Orientacion.M33 = V.Z;
            Orientacion.M34 = 0;

            Orientacion.M21 = Dir.X;
            Orientacion.M22 = Dir.Y;
            Orientacion.M23 = Dir.Z;
            Orientacion.M24 = 0;


            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            return Orientacion;
        }

        static public Matrix CalcularUVN(Vector3 Dir, Vector3 Up)
        {
            // determino la orientacion
            Dir.Normalize();
            Vector3 U = Vector3.Cross(Up, Dir);
            U.Normalize();
            Vector3 V = Vector3.Cross(Dir, U);
            V.Normalize();
            Matrix Orientacion;
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M31 = V.X;
            Orientacion.M32 = V.Y;
            Orientacion.M33 = V.Z;
            Orientacion.M34 = 0;

            Orientacion.M21 = Dir.X;
            Orientacion.M22 = Dir.Y;
            Orientacion.M23 = Dir.Z;
            Orientacion.M24 = 0;


            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            return Orientacion;
        }


        static public Matrix MatrixfromBasis(float Ux, float Uy, float Uz,
                                                float Vx, float Vy, float Vz,
                                                float Wx, float Wy, float Wz)
        {
            Matrix O = new Matrix();
            O.M11 = Ux; O.M12 = Uy; O.M13 = Uz; O.M14 = 0;
            O.M21 = Vx; O.M22 = Vy; O.M23 = Vz; O.M24 = 0;
            O.M31 = Wx; O.M32 = Wy; O.M33 = Wz; O.M34 = 0;
            O.M41 = 0; O.M42 = 0; O.M43 = 0; O.M44 = 1;
            return O;
        }

        static public Vector3 rotar(Vector3 A, Vector3 o, Vector3 eje, float theta)
        {
            float x = A.X;
            float y = A.Y;
            float z = A.Z;
            float a = o.X;
            float b = o.Y;
            float c = o.Z;
            float u = eje.X;
            float v = eje.Y;
            float w = eje.Z;

            float u2 = u * u;
            float v2 = v * v;
            float w2 = w * w;
            float cosT = FastMath.Cos(theta);
            float sinT = FastMath.Sin(theta);
            float l2 = u2 + v2 + w2;
            float l = FastMath.Sqrt(l2);

            if (l2 < 0.000000001f)       // el vector de rotacion es casi nulo
                return A;

            float xr = a * (v2 + w2) + u * (-b * v - c * w + u * x + v * y + w * z)
                    + (-a * (v2 + w2) + u * (b * v + c * w - v * y - w * z) + (v2 + w2) * x) * cosT
                    + l * (-c * v + b * w - w * y + v * z) * sinT;
            xr /= l2;

            float yr = b * (u2 + w2) + v * (-a * u - c * w + u * x + v * y + w * z)
                    + (-b * (u2 + w2) + v * (a * u + c * w - u * x - w * z) + (u2 + w2) * y) * cosT
                    + l * (c * u - a * w + w * x - u * z) * sinT;
            yr /= l2;

            float zr = c * (u2 + v2) + w * (-a * u - b * v + u * x + v * y + w * z)
                    + (-c * (u2 + v2) + w * (a * u + b * v - u * x - v * y) + (u2 + v2) * z) * cosT
                    + l * (-b * u + a * v - v * x + u * y) * sinT;
            zr /= l2;

            return new Vector3(xr, yr, zr);
        }
    }
}



