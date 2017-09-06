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
        public static float k = 0.11f ;
        public static int r = 8;
        public static float largo = 10*2*(r+1);
        public static float ancho = 10*2*(r+1);
        public static float alto = 20;
        public Vector3 Position;
        public Matrix Orient;
        public Vector3 escale = new Vector3(1, 1, 1) * k;
        public Matrix[] matWorld = new Matrix[500];
        public int[] mesh_index = new int[500];
        public int cant_mesh;
        public TgcBoundingAxisAlignBox BoundingBox;
        public GameModel model;
        public Matrix matWorldBock = new Matrix();
        public Matrix matWorldSurfaceBlock = new Matrix();
        public int tipo;


        public Block(Vector3 pos, GameModel pmodel, int ptipo)
        {
            model = pmodel;
            tipo = ptipo;
            Position = pos;
            int prof = 2;       // profundidad del trench

            Orient = Helper.CalcularUVN(pos);
            Matrix OrientTecho = Helper.MatrixfromBasis(1, 0, 0,
                                                            0, -1, 0,
                                                            0, 0, 1);

            Matrix OrientPared = Helper.MatrixfromBasis(1, 0, 0,
                                                            0, 0, -1,
                                                            0, 1, 0);

            Matrix OrientParedI = Helper.MatrixfromBasis(1, 0, 0,
                                                            0, 0, 1,
                                                            0, 1, 0);

            Matrix OrientParedU = Helper.MatrixfromBasis(0, 1, 0,
                                                         1, 0, 0,
                                                         0, 0, 1);

            Matrix OrientParedD = Helper.MatrixfromBasis(0, 1, 0,
                                                         -1, 0, 0,
                                                         0, 0, 1);
            Matrix OrientPipeline = Helper.MatrixfromBasis(0, 0, 1,
                                                         0, 1, 0,
                                                         1, 0, 0);

            Matrix MatPos = Matrix.Translation(pos);

            bool sin_salida = tipo==1?true:false;
            bool tunel = sin_salida ? false : random.Next(0, 4) == 0 ? true : false;
            
            // piso y techo
            int t = 0;
            if (tunel)
            {
                for (int i = -r; i < r; ++i)
                {
                    for (int j = -r; j < r; ++j)
                        if (i == 0 || i == -1 || j == 0 || j == -1)
                        {
                            // piso
                            matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), j * 10)) * Orient *MatPos;
                            ++t;

                            if (i != -r && i != r - 1 && j != -r && j != r - 1)
                            {
                                // techo
                                matWorld[t] = Matrix.Scaling(escale) * OrientTecho * Matrix.Translation(new Vector3(i * 10, 0, j * 10)) * Orient * MatPos;
                                ++t;
                            }
                        }
                }
            }
            else
            {
                for (int i = -r; i < r; ++i)
                {
                    for (int j = -r; j < r; ++j)
                        if ((i >= -2 && i <= 1) || (j >= -2 && j <= 1))
                        {

                            // 1-escalo
                            matWorld[t] = Matrix.Scaling(escale);
                            // 2-traslado en el espacio del bloque
                            if (i == 0 || i == -1 || j == 0 || j == -1)
                                //  piso
                                matWorld[t] = matWorld[t] * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), j * 10)) * Orient;
                            else
                                //  techo
                                matWorld[t] = matWorld[t] * Matrix.Translation(new Vector3(i * 10, 10, j * 10)) * Orient;

                            // 4- traslado a la pos. en world space
                            matWorld[t] = matWorld[t] * MatPos;
                            ++t;
                        }

                }
            }

            // pared
            for (int s = 0; s < prof; ++s)
            {
                for (int i = -r; i < r; ++i)
                    if (i != 0 && i != -1)
                    {

                        // pared der
                        matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, 5)) * Orient * MatPos;
                        // pared izq 
                        matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, -15)) * Orient * MatPos;
                        // pared U
                        matWorld[t++] = Matrix.Scaling(escale) * OrientParedU * Matrix.Translation(new Vector3(-15, 5 - 10 * s, i * 10)) * Orient * MatPos;
                        matWorld[t++] = Matrix.Scaling(escale) * OrientParedD * Matrix.Translation(new Vector3(5, 5 - 10 * s, i * 10)) * Orient * MatPos;
                    }

                if (sin_salida)
                {
                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(0, 5 - 10 * s, -10 * r-5)) * Orient * MatPos;
                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(-10, 5 - 10 * s, -10 * r-5)) * Orient * MatPos;

                    matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(0, 5 - 10 * s, 10 * r-5)) * Orient * MatPos;
                    matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(-10, 5 - 10 * s, 10 * r-5)) * Orient * MatPos;

                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedU * Matrix.Translation(new Vector3(-10 * r - 5, 5 - 10 * s, 0)) * Orient * MatPos;
                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedU * Matrix.Translation(new Vector3(-10 * r - 5, 5 - 10 * s, -10)) * Orient * MatPos;

                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedD * Matrix.Translation(new Vector3(10 * r - 5, 5 - 10 * s, 0)) * Orient * MatPos;
                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedD * Matrix.Translation(new Vector3(10 * r - 5, 5 - 10 * s, -10)) * Orient * MatPos;



                }

            }


                for (int i = 0; i < t; ++i)
                mesh_index[i] = random.Next(0, 4);

            // agrego los cuadrantes vacios
            matWorld[t] = Matrix.Scaling(new Vector3(10 * (r - 1) - 3, 19, 10 * (r - 1) - 3)) * Matrix.Translation(new Vector3(-r * 5 - 10, 0, -r * 5 - 10)) * Orient * MatPos;
            mesh_index[t++] = -1;
            matWorld[t] = Matrix.Scaling(new Vector3(10 * (r - 1) - 3  , 19, 10 * (r - 1) - 3)) * Matrix.Translation(new Vector3(r * 5 , 0, -r * 5 - 10)) * Orient * MatPos;
            mesh_index[t++] = -1;
            matWorld[t] = Matrix.Scaling(new Vector3(10 * (r - 1) - 3, 19, 10 * (r - 1) - 3)) * Matrix.Translation(new Vector3(-r * 5 - 10, 0, r * 5)) * Orient * MatPos;
            mesh_index[t++] = -1;
            matWorld[t] = Matrix.Scaling(new Vector3(10 * (r - 1) - 3, 19, 10 * (r - 1) - 3)) * Matrix.Translation(new Vector3(r * 5, 0, r * 5)) * Orient * MatPos;
            mesh_index[t++] = -1;

            // agrego el pipepeline
            if (random.Next(0, 5) == 0)
            {
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Scaling(new Vector3(7, 7, 7))
                    * Matrix.Translation(new Vector3(-2, 6, -30)) * Orient * MatPos;
                mesh_index[t++] = 6;

                matWorld[t] = Matrix.Scaling(escale) * Matrix.Scaling(new Vector3(7, 7, 7))
                    * Matrix.Translation(new Vector3(-2, 9, -32)) * Orient * MatPos;
                mesh_index[t++] = 6;

                matWorld[t] = Matrix.Scaling(escale) * Matrix.Scaling(new Vector3(7, 7, 7))
                    * Matrix.Translation(new Vector3(-2, 0, -32)) * OrientPipeline * Orient * MatPos;
                mesh_index[t++] = 6;
            }

            cant_mesh = t;

            // calculo el bounding box de toto el bloque
            Matrix T = Orient * MatPos;
            Vector3[] p = new Vector3[8];
            float min_x = 10000000, min_y = 10000000, min_z = 10000000;
            float max_x = -10000000, max_y = -10000000, max_z = -10000000;
            p[0] = new Vector3(-largo / 2, 0, -ancho / 2);
            p[1] = new Vector3(largo / 2, 0, -ancho / 2);
            p[2] = new Vector3(largo / 2, alto, -ancho / 2);
            p[3] = new Vector3(-largo / 2, alto, -ancho / 2);

            p[4] = new Vector3(-largo / 2, 0, ancho / 2);
            p[5] = new Vector3(largo / 2, 0, ancho / 2);
            p[6] = new Vector3(largo / 2, alto, ancho / 2);
            p[7] = new Vector3(-largo / 2, alto, ancho / 2);

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


        public bool render()
        {
            var r = TgcCollisionUtils.classifyFrustumAABB(model.Frustum, BoundingBox);
            if (r == TgcCollisionUtils.FrustumResult.OUTSIDE)
                return false;

            switch(tipo)
            {
                case 0:
                case 1:
                    // trench 
                    for (int i = 0; i < cant_mesh; ++i)
                    {
                        int index = mesh_index[i];
                        if (index != -1)
                        {
                            model.meshes[index].Transform = matWorld[i];
                            model.meshes[index].render();
                        }
                        else
                        {
                            model.Box.Transform = matWorld[i];
                            model.Box.render();
                        }
                    }
                    break;
                case 2:
                case 3:
                case 4:
                    // surface
                    foreach (TgcMesh mesh in model.surface)
                    {
                        mesh.Transform = matWorldSurfaceBlock;
                        mesh.render();
                    }
                    break;
                default:
                    model.Box.Transform = matWorldBock;
                    model.Box.render();
                    break;
            }

            if (model.curr_mode == defines.MODO_TEST_BLOCK)
                BoundingBox.render();

            return true;
        }




        }

        /// <summary>
        ///     Ejemplo para implementar el TP.
        ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
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


        public int curr_mode = defines.MODO_GAME;
        public bool first_person = false;

        public TgcBox Box { get; set; }
        public List<TgcMesh> meshes = new List<TgcMesh>();
        public List<TgcMesh> surface = new List<TgcMesh>();
        public TgcArrow ship = new TgcArrow();
        public TgcBox BlockSurface;
        public TgcBox BlockTrench;

        public float star_r = 1500;
        public Vector3 ship_vel = new Vector3(1, 0, 0);
        public float ship_speed = 100;
        public Vector3 ship_pos;
        public Vector3 cam_vel = new Vector3(0, 0, 0);
        public Vector3 cam_pos = new Vector3(1, 0, 0);
        public Vector3 target_pos = new Vector3(1, 0, 0);
        public List<Block> scene = new List<Block>();
        public TgcBox LightBox;
        public Effect currentShader;
        public TgcSkyBox skyBox;
        private static readonly Random random = new Random();

        //Boleano para ver si dibujamos el boundingbox
        private bool BoundingBox { get; set; }

        public float xm, ym;     // pos del mouse
        public float wm;
        public int eventoInterno = 0;

        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aquí todo el código de inicialización: cargar modelos, texturas, estructuras de optimización, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {
            //Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;
            Box = TgcBox.fromSize(new Vector3(1, 1, 1), TgcTexture.createTexture(MediaDir + "Textures\\23.png"));
            Box.Position = new Vector3(0, 0, 0);

            BlockSurface = TgcBox.fromSize(new Vector3(1, 1, 1), TgcTexture.createTexture(MediaDir + "Textures\\ds_surface.png"));
            BlockTrench = TgcBox.fromSize(new Vector3(1, 1, 1), TgcTexture.createTexture(MediaDir + "Textures\\ds_trench.png"));

            //currentShader = TgcShaders.Instance.TgcMeshSpotLightShader;
            currentShader = TgcShaders.Instance.TgcMeshPointLightShader;
            var loader = new TgcSceneLoader();
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m1-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m2-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m3-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m4-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m5-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "Turbolaser-TgcScene.xml").Meshes[0]);       // 5
            meshes.Add(loader.loadSceneFromFile(MediaDir + "tuberia-TgcScene.xml").Meshes[0]);          // 6


            foreach (TgcMesh mesh in meshes)
            {
                mesh.Scale = new Vector3(1f, 1f, 1f);
                mesh.AutoTransformEnable = false;
                mesh.Effect = currentShader;
                mesh.Technique = TgcShaders.Instance.getTgcMeshTechnique(mesh.RenderType);
            }


            surface = loader.loadSceneFromFile(MediaDir + "death+star2-TgcScene.xml").Meshes;
            foreach (TgcMesh mesh in surface)
            {
                mesh.AutoTransformEnable = false;
                mesh.Effect = currentShader;
                mesh.Technique = TgcShaders.Instance.getTgcMeshTechnique(mesh.RenderType);
            }


            if (curr_mode==defines.MODO_TEST_BLOCK)
            {
                scene.Add(new Block(new Vector3(0, 100, 0) , this, 0));
            }
            else
            {
                ArmarBanda(0, 2 * FastMath.PI, 0, FastMath.PI);
            }

            Vector3 LP = new Vector3(0, star_r * 5, star_r);
            Vector3 cameraPosition;
            Vector3 lookAt;
            if (curr_mode == defines.MODO_GAME)
            {
                float ship_alfa = 1;
                float ship_beta = 1.5f;
                float x = FastMath.Cos(ship_alfa) * FastMath.Sin(ship_beta);
                float y = FastMath.Sin(ship_alfa) * FastMath.Sin(ship_beta);
                float z = FastMath.Cos(ship_beta);
                ship_pos = new Vector3(x, y, z) * (star_r + 20);
                Vector3 N = ship_pos;
                N.Normalize();
                ship_vel = Vector3.Cross(N, new Vector3(0, 1, 0));
                cam_pos = ship_pos - ship_vel * 100 + N * 20;
                target_pos = ship_pos;

                if (!first_person)
                {
                    Vector3 bitan = Vector3.Cross(N, ship_vel);
                    Vector3 tg = Vector3.Cross(N, bitan);
                    Vector3 surface_vel = bitan * Vector3.Dot(ship_vel, bitan) + tg * Vector3.Dot(ship_vel, tg);
                    surface_vel.Normalize();
                    Camara.SetCamera(ship_pos - surface_vel * 50 + 20 * N, ship_pos + N * 10);
                }


            }
            else
            {
                if (curr_mode == defines.MODO_TEST_BLOCK)
                {
                    LP = new Vector3(0, 150, 0);
                    cameraPosition = new Vector3(0, 140, 200);
                    lookAt = new Vector3(0, 0, 0);
                }
                else
                {
                    cameraPosition = new Vector3(0, 0, star_r * 2);
                    lookAt = new Vector3(0, 0, 0);
                }
                Camara.SetCamera(cameraPosition, lookAt);
            }


            //Cargar variables shader de la luz
            currentShader.SetValue("lightColor", ColorValue.FromColor(Color.FromArgb(240, 240, 255)));
            currentShader.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(LP));
            currentShader.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
            currentShader.SetValue("lightIntensity", (float)1);
            currentShader.SetValue("lightAttenuation", (float)0);

            //Cargar variables de shader de Material. El Material en realidad deberia ser propio de cada mesh. Pero en este ejemplo se simplifica con uno comun para todos
            currentShader.SetValue("materialEmissiveColor", ColorValue.FromColor(Color.FromArgb(0, 0, 0)));
            currentShader.SetValue("materialAmbientColor", ColorValue.FromColor(Color.FromArgb(40, 40, 40)));
            currentShader.SetValue("materialDiffuseColor", ColorValue.FromColor(Color.FromArgb(180, 180, 180)));
            currentShader.SetValue("materialSpecularColor", ColorValue.FromColor(Color.FromArgb(240,240,240)));
            currentShader.SetValue("materialSpecularExp", (float)10);
            currentShader.SetValue("specularFactor", (float)0.7);


            LightBox = TgcBox.fromSize(LP, curr_mode == defines.MODO_TEST_BLOCK ? new Vector3(10,10,10) : new Vector3(100,100,100));
            LightBox.AutoTransformEnable = true;

            xm = Input.Xpos;
            ym = Input.Ypos;
            wm = Input.WheelPos;

            //Crear SkyBox
            skyBox = new TgcSkyBox();
            skyBox.Center = new Vector3(0, 0, 0);
            skyBox.Size = new Vector3(10000, 10000, 10000);
            var texturesPath = MediaDir + "Textures\\SkyBox2\\";
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "lun4_up.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "lun4_dn.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "lun4_lf.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "lun4_rt.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "lun4_bk.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "lun4_ft.jpg");
            skyBox.Init();

            

        }

        public void ArmarBanda(float alfa_0 , float alfa_1 , float beta_0 , float beta_1)
        {
            int largo = 6;
            int l = 0;
            float cant_j = (int)(star_r * (beta_1 - beta_0) / (Block.ancho * 0.9f));
            for (int j = 0; j < cant_j; ++j)
            {
                float tj = (float)j / (float)cant_j;
                float beta = beta_0 * (1 - tj) + beta_1 * tj;
                float radio = FastMath.Sin(beta) * star_r;

                float cant_i = (int)(radio * (alfa_1 - alfa_0) / (Block.largo * 0.9f));
                for (int i = 0; i < cant_i; ++i)
                {
                    float ti = (float)i / (float)cant_i;
                    float alfa = alfa_0 * (1 - ti) + alfa_1 * ti;
                    float x = FastMath.Cos(alfa) * FastMath.Sin(beta);
                    float y = FastMath.Sin(alfa) * FastMath.Sin(beta);
                    float z = FastMath.Cos(beta);
                    int tipo = Math.Abs(j- cant_j/2) <2 ?
                        (l++ % largo) != 0 ? 0 : 1: 
                        2 + random.Next(0, 6);
                    scene.Add(new Block(new Vector3(x, y, z) * star_r, this,tipo));
                }
            }
        }


        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la lógica de computo del modelo, así como también verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        public override void Update()
        {
            PreUpdate();

            if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.F))
                first_person = !first_person;
            if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.C))
            { 
                curr_mode = (curr_mode+1)%2;
                if(curr_mode == defines.MODO_CAMARA)
                    Camara.SetCamera(new Vector3(0, 0, star_r * 2), new Vector3(0, 0, 0));
            }

            if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_RIGHT))
                ElapsedTime = 0;

                if (Input.buttonUp(TgcD3dInput.MouseButtons.BUTTON_LEFT) || Input.buttonUp(TgcD3dInput.MouseButtons.BUTTON_MIDDLE))
            {
                eventoInterno = 0;
            }

            if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
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

                    if (curr_mode == defines.MODO_GAME)
                    {
                        if (!Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift))
                        {
                            dx *= 0.3f;
                            dy *= 0.5f;
                        }
                        Vector3 Up = ship_pos;
                        Up.Normalize();
                        Vector3 tg = Vector3.Cross(Up, ship_vel);
                        Vector3 N = Vector3.Cross(ship_vel, tg);
                        Matrix rot = Matrix.RotationAxis(N, ElapsedTime * dx);
                        ship_vel.TransformNormal(rot);
                        Matrix rotY = Matrix.RotationAxis(tg, ElapsedTime * dy);
                        ship_vel.TransformNormal(rotY);
                    }
                    else
                    {
                        // uso el desplazamiento en x para rotar el punto de vista 
                        // en el plano xy
                        float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift) ? 0.05f : 0.5f;
                        float tot_x = 800;
                        float an = dx / tot_x * 2 * FastMath.PI * k;
                        Matrix T = Matrix.RotationY(an);
                        Vector3 LF = Camara.Position;
                        LF.TransformCoordinate(T);

                        Vector3 ViewDir = Camara.LookAt - Camara.Position;
                        ViewDir.Normalize();

                        Vector3 N;
                         N = Vector3.Cross(new Vector3(0, 1, 0) , ViewDir);

                        float tot_y = 600;
                        float an_y = dy / tot_y * FastMath.PI * k ;
                        LF = Helper.rotar(LF , Camara.LookAt, N, an_y);

                        Camara.SetCamera(LF, Camara.LookAt);


                    }
                }
            }


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

                    float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftControl) ? 0.1f : 1f;

                    Vector3 VUP = new Vector3(0,1,0);
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
                    Vector3 n = Vector3.Cross(d , VUP);
                    n.Normalize();
                    Vector3 up = Vector3.Cross(n, d);
                    Vector3 desf = up * (dy * ky) + n * (dx * kx);
                    Camara.SetCamera(Camara.Position + desf, Camara.LookAt+desf);
                }
            }

            float zDelta = Input.WheelPos;
            wm = Input.WheelPos;
            if(FastMath.Abs(zDelta)>0.1f)
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
                if (ElapsedTime < 10)
                {
                    float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift) ? 1 : 0.4f;
                    Vector3 ant_ship_pos = ship_pos;
                    ship_pos = ship_pos + ship_vel * ElapsedTime*ship_speed*k;
                    Vector3 N = ship_pos;
                    float dist = N.Length();
                    float dist_min = star_r - 10;
                    float dist_max = star_r + 50;
                    N.Normalize();
                    Vector3 bitan = Vector3.Cross(N, ship_vel);
                    Vector3 tg = Vector3.Cross(N, bitan);
                    Vector3 surface_vel = bitan * Vector3.Dot(ship_vel, bitan) + tg * Vector3.Dot(ship_vel, tg);
                    surface_vel.Normalize();


                    if (dist < dist_min)
                    {
                        ship_pos = N * dist_min;
                        ship_vel = surface_vel;
                    }
                    else
                    if (dist > dist_max)
                    {
                        ship_pos = N * dist_max;
                        ship_vel = surface_vel;
                    }

                    if (first_person)
                    {
                        Vector3 pvision = ship_pos + ship_vel * 200;
                        pvision.Normalize();
                        Camara.SetCamera(ship_pos, pvision * star_r + N * 10, N);
                    }
                    else
                    {
                        Vector3 LA, LF;
                        Vector3 ViewDir = Camara.LookAt - Camara.Position;
                        float cam_dist = ViewDir.Length();
                        ViewDir.Normalize();
                        Vector3 desired_LF = ship_pos - surface_vel * 5 + N * 1.8f;
                        Vector3 desired_LA = ship_pos + N * 2.0f;
                        Vector3 desired_ViewDir = desired_LA - desired_LF;
                        desired_ViewDir.Normalize();

                        float st = .3f;
                        Quaternion q0 = new Quaternion(ViewDir.X, ViewDir.Y, ViewDir.Z, 0);
                        Quaternion q1 = new Quaternion(desired_ViewDir.X, desired_ViewDir.Y, desired_ViewDir.Z, 0);
                        Quaternion qf = Quaternion.Slerp(q0, q1, st);
                        Vector3 qViewDir = new Vector3(qf.X, qf.Y, qf.Z);
                        float SPRING_CONSTANT = 45.0f;      // elasticidad
                        float DAMPING_CONSTANT = 10.0f;      // amortiguacion
                        var displacement = Camara.Position - desired_LF;
                        var springAcceleration = -SPRING_CONSTANT * displacement - DAMPING_CONSTANT * cam_vel;
                        cam_vel += springAcceleration * ElapsedTime;
                        LF = Camara.Position + cam_vel * ElapsedTime;
                        //LF = desired_LF * 0.3f + Camara.Position * 0.7f;

                        LA = LF + qViewDir * cam_dist;
                        Camara.SetCamera(LF, LA, N);
                        //Camara.SetCamera(desired_LF, desired_LA, N);
                    }
                }

            }

        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aquí todo el código referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>
        public override void Render()
        {
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();


            currentShader.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(Camara.Position));

            switch (curr_mode)
            {
                case defines.MODO_TEST_BLOCK:
                    scene[0].render();
                    //Box.Transform = scene[0].matWorldBock;
                    //Box.render();
                    Vector3 Q = Camara.Position;
                    DrawText.drawText("X=" + Q.X + "  Y=" + Q.Y + "  Z=" + Q.Z, 5, 20, Color.Yellow);
                    DrawText.drawText("TEST BLOCK", 700, 30, Color.Yellow);
                    break;
                case defines.MODO_GAME:
                    {
                        //Render SkyBox
                        skyBox.render();
                        int cant_dibujados = 0;
                        float dist_lod = 300000;
                        foreach (Block bloque in scene)
                        {
                            if ((bloque.Position - Camara.Position).LengthSq() < dist_lod)
                            {
                                if (bloque.render())
                                    cant_dibujados++;
                            }
                            else
                            {
                                //Box.Transform = bloque.matWorldBock;
                                //Box.render();
                            }
                        }

                        LightBox.render();
                        if (!first_person)
                        {
                            ship.PStart = ship_pos;
                            ship.PEnd = ship_pos + ship_vel * 5;
                            ship.updateValues();
                            ship.render();
                        }

                        DrawText.drawText("Cant: " + cant_dibujados, 5, 20, Color.Yellow);
                        DrawText.drawText("F -> toogle first person", 700, 20, Color.Yellow);
                        DrawText.drawText("GAME MODE    (shift accel)", 700, 30, Color.Yellow);


                    }
                    break;
                case defines.MODO_CAMARA:
                default:
                    {
                        foreach (Block bloque in scene)
                        {
                            TgcBox pbox;
                            switch (bloque.tipo)
                            {
                                case 0:
                                    pbox = BlockTrench;
                                    break;
                                default:
                                    pbox = BlockSurface;
                                    break;

                            }
                            pbox.Transform = bloque.matWorldBock;
                            pbox.render();
                        }
                    }
                    DrawText.drawText("CAMARA MODE   (shift mas lenta)", 700, 30, Color.Yellow);
                    break;
            }

            DrawText.drawText("C->Toogle Camara mode" , 400, 10, Color.Yellow);



            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();

        }

        /// <summary>
        ///     Se llama cuando termina la ejecución del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gráficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            Box.dispose();
            skyBox.dispose();
            foreach (TgcMesh mesh in meshes)
                mesh.dispose();
            foreach (TgcMesh mesh in surface)
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
            Vector3 U = Vector3.Cross(new Vector3(0, 0, 1), Dir);
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

        static public Matrix MatrixfromBasis(   float Ux, float Uy, float Uz,
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



// codigo deprecado
/*
public BlockImpar(Vector3 pos, GameModel pmodel, int ptipo)
{
    model = pmodel;
    tipo = ptipo;
    Position = pos;
    Orient = Helper.CalcularUVN(pos);
    Matrix OrientPiso = Matrix.Identity;
    Matrix OrientPared = Helper.MatrixfromBasis(    1,0,0,  
                                                    0,0,-1,   
                                                    0,1,0);

    Matrix OrientParedI = Helper.MatrixfromBasis(   1, 0, 0, 
                                                    0, 0, 1, 
                                                    0, 1, 0);

    Matrix OrientParedU = Helper.MatrixfromBasis(0, 1, 0,
                                                 1, 0, 0,
                                                 0, 0, 1);

    Matrix OrientParedD = Helper.MatrixfromBasis(0, 1, 0,
                                                 -1, 0, 0,
                                                 0, 0, 1);

    Matrix MatPos = Matrix.Translation(pos);

    int t = 0;
    // piso y techo
    for (int i=-r; i <= r; ++i)
    {
        for (int j = -r; j <= r; ++j)
        {
            // 1-escalo
            matWorld[t] = Matrix.Scaling(escale);
            // 2-traslado en el espacio del bloque
            matWorld[t] = matWorld[t] * Matrix.Translation(new Vector3(i * 10, i==0 || j==0 ? 0 : 10, j * 10));
            // 3- oriento en el spacio del bloque y luego en world space
            matWorld[t] = matWorld[t] * OrientPiso * Orient;
            // 4- traslado a la pos. en world space
            matWorld[t] = matWorld[t] * MatPos;
            ++t;
        }

    }

    // pared
    for (int i = -r; i <= r; ++i)
    if (i!=0 )
    {

        // pared der
        matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(i*10, 5, 5)) * Orient * MatPos;
        // pared izq 
        matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(i*10, 5, -5)) * Orient * MatPos;
        // pared U
        matWorld[t++] = Matrix.Scaling(escale) * OrientParedU * Matrix.Translation(new Vector3(-5, 5, i * 10)) * Orient * MatPos;
        matWorld[t++] = Matrix.Scaling(escale) * OrientParedD * Matrix.Translation(new Vector3(5, 5, i * 10)) * Orient * MatPos;
    }
    cant_mesh = t;

    for(int i=0;i<cant_mesh;++i)
        mesh_index[i] = random.Next(0, 4);


    // calculo el bounding box de toto el bloque
    Matrix T = Orient * MatPos;
    Vector3[] p = new Vector3[8];
    float min_x = 10000000, min_y = 10000000, min_z = 10000000;
    float max_x = -10000000, max_y = -10000000, max_z = -10000000;
    p[0] = new Vector3(-largo/2, 0, -ancho / 2);
    p[1] = new Vector3(largo  / 2, 0, -ancho  / 2);
    p[2] = new Vector3(largo  / 2, alto, -ancho / 2);
    p[3] = new Vector3(-largo  / 2, alto , -ancho / 2);

    p[4] = new Vector3(-largo  / 2, 0, ancho / 2);
    p[5] = new Vector3(largo  / 2, 0, ancho / 2);
    p[6] = new Vector3(largo  / 2, alto , ancho / 2);
    p[7] = new Vector3(-largo / 2, alto, ancho / 2);

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
    matWorldBock = Matrix.Scaling(new Vector3(largo,alto,ancho)) * T;

    if (tipo ==1  || tipo == 2)
        matWorldBock = Matrix.Translation(0, 7, 0) *
            Matrix.Scaling(new Vector3(0.01f, 0.1f, 0.01f)) * matWorldBock;
}*/
