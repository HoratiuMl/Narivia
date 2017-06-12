﻿using System;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using Narivia.Graphics;
using Narivia.Helpers;
using Narivia.Settings;

namespace Narivia.Screens
{
    /// <summary>
    /// Screen manager.
    /// </summary>
    public class ScreenManager
    {
        static volatile ScreenManager instance;
        static object syncRoot = new object();

        Screen currentScreen, newScreen;

        readonly XmlManager<Screen> xmlScreenManager;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ScreenManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            XmlManager<ScreenManager> xmlManager = new XmlManager<ScreenManager>();
                            instance = xmlManager.Load("Screens/ScreenManager.xml");
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Gets the content.
        /// </summary>
        /// <value>The content.</value>
        [XmlIgnore]
        public ContentManager Content { get; private set; }

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>The size.</value>
        [XmlIgnore]
        public Vector2 Size { get; private set; }

        /// <summary>
        /// Gets or sets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        [XmlIgnore]
        public GraphicsDevice GraphicsDevice { get; set; }

        /// <summary>
        /// Gets or sets the sprite batch.
        /// </summary>
        /// <value>The sprite batch.</value>
        [XmlIgnore]
        public SpriteBatch SpriteBatch { get; set; }

        /// <summary>
        /// Gets a value indicating whether the current screen is transitioning.
        /// </summary>
        /// <value><c>true</c> if transitioning; otherwise, <c>false</c>.</value>
        [XmlIgnore]
        public bool Transitioning { get; private set; }

        /// <summary>
        /// Gets or sets the image.
        /// </summary>
        /// <value>The image.</value>
        public Image TransitionImage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenManager"/> class.
        /// </summary>
        public ScreenManager()
        {
            Size = SettingsManager.Instance.Resolution;
            currentScreen = new SplashScreen();

            xmlScreenManager = new XmlManager<Screen>();
            xmlScreenManager.Type = currentScreen.Type;

            currentScreen = xmlScreenManager.Load("Screens/SplashScreen.xml");
        }

        /// <summary>
        /// Loads the content.
        /// </summary>
        /// <param name="content">Content.</param>
        public void LoadContent(ContentManager content)
        {
            Content = new ContentManager(content.ServiceProvider, "Content");

            currentScreen.LoadContent();
            TransitionImage.LoadContent();
        }

        /// <summary>
        /// Unloads the content.
        /// </summary>
        public void UnloadContent()
        {
            currentScreen.UnloadContent();
            TransitionImage.UnloadContent();
        }

        /// <summary>
        /// Updates the content.
        /// </summary>
        /// <param name="gameTime">Game time.</param>
        public void Update(GameTime gameTime)
        {
            if (Transitioning)
            {
                Transition(gameTime);
                return;
            }

            Size = SettingsManager.Instance.Resolution;
            TransitionImage.Scale = Size;

            currentScreen.Update(gameTime);
        }

        /// <summary>
        /// Draw the content on the specified spriteBatch.
        /// </summary>
        /// <param name="spriteBatch">Sprite batch.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            currentScreen.Draw(spriteBatch);

            if (Transitioning)
            {
                TransitionImage.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Changes the screen.
        /// </summary>
        /// <param name="screenName">Screen name.</param>
        public void ChangeScreens(string screenName)
        {
            newScreen = (Screen)Activator.CreateInstance(Type.GetType("Narivia.Screens." + screenName));

            TransitionImage.ActivateEffect("FadeEffect");
            TransitionImage.Active = true;
            TransitionImage.FadeEffect.Increasing = true;
            TransitionImage.Opacity = 0.0f;

            Transitioning = true;
        }

        /// <summary>
        /// Transitions to the new screen.
        /// </summary>
        /// <param name="gameTime">Game time.</param>
        void Transition(GameTime gameTime)
        {
            TransitionImage.Update(gameTime);

            if (TransitionImage.Opacity >= 1.0f)
            {
                currentScreen.UnloadContent();
                currentScreen = newScreen;
                xmlScreenManager.Type = currentScreen.Type;

                if (File.Exists(currentScreen.XmlPath))
                {
                    currentScreen = xmlScreenManager.Load(currentScreen.XmlPath);
                }

                currentScreen.LoadContent();
            }
            else if (TransitionImage.Opacity <= 0.0f)
            {
                TransitionImage.Active = false;
                Transitioning = false;
            }
        }
    }
}

