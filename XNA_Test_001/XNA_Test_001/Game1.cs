using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using FarseerPhysics;
using GeneticNN;

namespace Walk_ANN
{
    public class Game1 : Game
    {

        #region Declarations

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        World world;
        Body ground, lb1, lb2, lb3, rb1, rb2, rb3, head;
        Texture2D groundTexture, boneTexture, headTexture, footTexture;
        JointData[] leftJoints = new JointData[3];
        JointData[] rightJoints = new JointData[3];
        SpriteFont font;
        Vector2 offset, resolution = new Vector2(12f, 8f);
        BackgroundAnimation bkgAni;
        GeneticNeuralNetwork neural_net;
        double fps = 20f;
        bool controllerFlag = true;
        string outputMsg;

        #endregion

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);

            //Dimensions of output window
            graphics.PreferredBackBufferHeight = (int)(resolution.Y * 100f);
            graphics.PreferredBackBufferWidth = (int)(resolution.X * 100f);

            Content.RootDirectory = "Content";

            neural_net = new GeneticNeuralNetwork(9, 6, 100);
            this.TargetElapsedTime = TimeSpan.FromSeconds(1f / (fps * 60f));
        }

        protected override void Initialize()
        {
            this.IsMouseVisible = true;
            base.Initialize();
        }

        public void Reset()
        {
            world.Clear();
            LoadContent();
        }

        private float ConvertPixelToFloat(int a)
        {
            return a / 100.0f;
        }

        private float DegreeToRad(float deg)
        {
            return deg * 3.14f / 180f;
        }

        private Body ConstructFromTexture(Texture2D texture, bool flag, Vector2 pos, float density = 1f)
        {
            Body body;
            body = BodyFactory.CreateRectangle(world, ConvertPixelToFloat(texture.Width),
               ConvertPixelToFloat(texture.Height), density);
            if (flag)
                body.BodyType = BodyType.Static;
            else
                body.BodyType = BodyType.Dynamic;
            body.Position = pos;
            return body;
        }

        private AngleJoint CreateJoint(Texture2D texture1, Texture2D texture2, Body b1, ref Body b2, bool flag = true)
        {
            b2.Position = new Vector2(b1.Position.X, b1.Position.Y + texture1.Height / 100f);
            AngleJoint joint;
            if (flag)
                JointFactory.CreateRevoluteJoint(world, b1, b2,
                        new Vector2(0f, texture1.Height / 200f),
                        new Vector2(0f, -texture2.Height / 200f));
            else
                JointFactory.CreateRevoluteJoint(world, b1, b2,
                        new Vector2(0f, texture1.Height / 200f),
                        new Vector2(0f, -texture2.Width / 200f));
            joint = JointFactory.CreateAngleJoint(world, b1, b2);
            joint.TargetAngle = DegreeToRad(0f);
            joint.MaxImpulse = 600000000f;
            return joint;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            if (world == null)
                world = new World(new Vector2(0, 1f));
            else
                world.Clear();


            #region Loads Content Data

            font = Content.Load<SpriteFont>("font");
            boneTexture = Content.Load<Texture2D>("bone");
            groundTexture = Content.Load<Texture2D>("ground");
            headTexture = Content.Load<Texture2D>("head");
            footTexture = Content.Load<Texture2D>("foot");

            #endregion

            #region Creates Skeleton

            float shift = 3.0f;
            ground = ConstructFromTexture(groundTexture, true, new Vector2(6f, 8f));

            lb1 = ConstructFromTexture(boneTexture, false, new Vector2(1.0f, 0f + shift));
            lb2 = ConstructFromTexture(boneTexture, false, new Vector2(2.0f, 0f + shift));
            rb1 = ConstructFromTexture(boneTexture, false, new Vector2(3.0f, 0f + shift));
            rb2 = ConstructFromTexture(boneTexture, false, new Vector2(4.0f, 0f + shift));

            lb3 = ConstructFromTexture(boneTexture, false, new Vector2(5.0f, 2f + shift));
            rb3 = ConstructFromTexture(boneTexture, false, new Vector2(6.0f, 2f + shift));

            head = ConstructFromTexture(headTexture, false, new Vector2(8.0f, 1f + shift), 0.5f);
            head.Inertia = 10f;

            lb3.Friction = 10f;
            rb3.Friction = 10f;
            ground.Friction = 10f;

            #endregion

            #region    Declares Collision Category

            lb1.CollisionCategories = lb2.CollisionCategories = lb3.CollisionCategories = Category.Cat1;
            rb1.CollisionCategories = rb2.CollisionCategories = rb3.CollisionCategories = Category.Cat2;

            lb1.CollidesWith = lb2.CollidesWith = lb3.CollidesWith = Category.Cat1 | Category.Cat4;
            rb1.CollidesWith = rb2.CollidesWith = rb3.CollidesWith = Category.Cat2 | Category.Cat4;

            head.CollisionCategories = Category.Cat3;
            head.CollidesWith = Category.Cat4;

            ground.CollisionCategories = Category.Cat4;
            ground.CollidesWith = Category.Cat1 | Category.Cat2 | Category.Cat3 | Category.Cat4;

            #endregion

            #region Declares joints

            for (int i = 0; i <= 2; i++)
            {
                leftJoints[i] = new JointData();
                rightJoints[i] = new JointData();
            }

            leftJoints[0].CreateJoint(world, headTexture, boneTexture, head, ref lb1, true, DegreeToRad(30f), DegreeToRad(-90f));
            rightJoints[0].CreateJoint(world, headTexture, boneTexture, head, ref rb1, true, DegreeToRad(30f), DegreeToRad(-90f));

            leftJoints[1].CreateJoint(world, boneTexture, boneTexture, lb1, ref lb2, true, DegreeToRad(160f), DegreeToRad(-10f));
            rightJoints[1].CreateJoint(world, boneTexture, boneTexture, rb1, ref rb2, true, DegreeToRad(160f), DegreeToRad(-10f));

            lb3.Rotation = DegreeToRad(90f);
            rb3.Rotation = DegreeToRad(90f);

            leftJoints[2].CreateJoint(world, boneTexture, boneTexture, lb2, ref lb3, false, DegreeToRad(275f), DegreeToRad(-30f));
            rightJoints[2].CreateJoint(world, boneTexture, boneTexture, rb2, ref rb3, false, DegreeToRad(275f), DegreeToRad(-30f));

            #endregion

            #region Joint's Angle Value

            leftJoints[0].Set(DegreeToRad(-45f));
            rightJoints[0].Set(DegreeToRad(-45f));

            leftJoints[1].Set(DegreeToRad(75f));
            rightJoints[1].Set(DegreeToRad(75f));

            leftJoints[2].Set(DegreeToRad(50f));
            rightJoints[2].Set(DegreeToRad(50f));
            //la1.Softness = la2.Softness = la3.Softness = 0.2f;
            //ra1.Softness = ra2.Softness = ra3.Softness = 0.2f;

            #endregion

            bkgAni = new BackgroundAnimation(new Texture2D[] { groundTexture, Content.Load<Texture2D>("bkg") },
                                             new Vector2[] { new Vector2(6f, 7.5f), new Vector2(0f, 2.7f) }, resolution, head.Position);

            //Thread thread = new Thread(new ThreadStart(Controller));
            //thread.Start();
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            offset.Y = 0f;
            offset.X = -head.Position.X + 5f;

            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.U))
                this.TargetElapsedTime = TimeSpan.FromSeconds(1f / (20 * 60f));
            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.I))
                this.TargetElapsedTime = TimeSpan.FromSeconds(1f / 60f);

            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Q))
            {
                Console.WriteLine("T H A N K     Y O U ! ! ! !");
                Thread.Sleep(1000);
                Exit();
            }

            //if (Mouse.GetState().LeftButton.Equals(KeyState.Down))
            Message msg = neural_net.msg;

            double score = head.Position.X - 7f;

            if (msg == Message.RESET)
            {
                Console.WriteLine("RESET");
                neural_net.UpdatePool(score);
                Reset();
                Console.Clear();
                neural_net.DisplayGenome();
            }

            #region INPUT LAYER

            List<double> input = new List<double>();
            input.Add(head.Position.X);
            input.Add(head.Position.Y);
            input.Add(head.AngularVelocity);
            for (int i = 0; i < 3; i++)
                input.Add(leftJoints[i].Get());
            for (int i = 0; i < 3; i++)
                input.Add(rightJoints[i].Get());

            #endregion

            #region OUTPUT LAYER

            List<double> output = neural_net.Iterate(input);
            foreach (double i in output)
            {
                //Console.Write(i + " ");
            }

            //Console.WriteLine(" ");

            for (int i = 0; i < 6; i++)
            {
                if (i < 3)
                    leftJoints[i].Update((float)output[i]);
                else
                    rightJoints[i / 2].Update((float)output[i / 2]);
            }

            #endregion

            world.Step(1 / 30f);

            outputMsg = neural_net.generation + " ";
            outputMsg += neural_net.currentGenome + " ";
            outputMsg += neural_net.currentSample + " ";
            outputMsg += score;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            DrawScreen();
            base.Draw(gameTime);
        }

        protected void DrawBody(Texture2D texture, Body body)
        {
            Vector2 position = body.Position + offset;
            spriteBatch.Draw(texture, ConvertUnits.ToDisplayUnits(position), null, Color.White, body.Rotation,
                new Vector2(texture.Width / 2.0f, texture.Height / 2.0f), 1f,
                SpriteEffects.None, 0f);
        }

        private void DrawScreen()
        {
            //Console.WriteLine(rect1.Position.X + "   " + rect1.Position.Y);
            GraphicsDevice.Clear(Color.White);

            spriteBatch.Begin();

            bkgAni.Draw(head.Position, spriteBatch);

            DrawBody(boneTexture, lb1);
            DrawBody(boneTexture, lb2);
            DrawBody(boneTexture, lb3);

            DrawBody(headTexture, head);

            DrawBody(boneTexture, rb1);
            DrawBody(boneTexture, rb2);
            DrawBody(boneTexture, rb3);

            //DrawBody(groundTexture, ground);

            spriteBatch.DrawString(font, head.Position.X + "", new Vector2(0f, 50), Color.Red);
            spriteBatch.DrawString(font, outputMsg, new Vector2(0f, 100), Color.Red);

            spriteBatch.End();
        }


    }

    public class JointData
    {
        public AngleJoint joint;
        float upperLimit, lowerLimit, currAngle = 0;

        public void CreateJoint(World world, Texture2D texture1, Texture2D texture2,
                                Body b1, ref Body b2, bool flag, float uL, float lL)
        {
            b2.Position = new Vector2(b1.Position.X, b1.Position.Y + texture1.Height / 100f);
            if (flag)
                JointFactory.CreateRevoluteJoint(world, b1, b2,
                        new Vector2(0f, texture1.Height / 200f),
                        new Vector2(0f, -texture2.Height / 200f));
            else
                JointFactory.CreateRevoluteJoint(world, b1, b2,
                        new Vector2(0f, texture1.Height / 200f),
                        new Vector2(0f, -texture2.Width / 200f));
            joint = JointFactory.CreateAngleJoint(world, b1, b2);
            joint.TargetAngle = 0f;
            joint.MaxImpulse = 0.02f;
            upperLimit = uL;
            lowerLimit = lL;
            joint.CollideConnected = false;
        }

        public void Update(float val)
        {
            currAngle += val;
            if (currAngle > upperLimit)
                currAngle = upperLimit;
            if (currAngle < lowerLimit)
                currAngle = lowerLimit;
            joint.TargetAngle = currAngle;
        }

        public void Set(float angle)
        {
            currAngle = angle;
            if (currAngle > upperLimit)
                currAngle = upperLimit;
            if (currAngle < lowerLimit)
                currAngle = lowerLimit;
            joint.TargetAngle = currAngle;
        }

        public float Get()
        {
            return currAngle;
        }
    }

    public class BackgroundAnimation
    {
        #region Declarations

        Texture2D[] texture;    // Background Sprites
        Vector2[] position;     // Postion of Each Image
        int length { get; }     // No of layers
        Vector2 resolution, posRef; // Resolution of output screen
        float xRef; // Reference Line

        #endregion

        public BackgroundAnimation(Texture2D[] texture, Vector2[] position, Vector2 resolution, Vector2 posRef)
        {
            this.texture = texture;
            this.length = texture.Length;
            this.position = new Vector2[length];
            for (int i = 0; i < length; i++)
                this.position[i] = position[i];
            this.resolution = resolution;
            xRef = 0;
            this.posRef = posRef;
        }

        public void Draw(Vector2 currPostion, SpriteBatch spriteBatch)
        {
            xRef += posRef.X - currPostion.X;
            posRef = currPostion;
            for (int i = length - 1; i >= 0; i--)
            {
                xRef /= i + 1;
                int n = (int)(xRef / (texture[i].Width / 100f));
                for (int j = 0; j < 2; j++)
                {
                    spriteBatch.Draw(texture[i], ConvertUnits.ToDisplayUnits(new Vector2(xRef - (n + j) * texture[i].Width / 100f, position[i].Y)), Color.White);
                    spriteBatch.Draw(texture[i], ConvertUnits.ToDisplayUnits(new Vector2(xRef + (n + j) * texture[i].Width / 100f, position[i].Y)), Color.White);
                }
                xRef *= i + 1;
            }
        }
    }
}
