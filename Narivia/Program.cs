﻿#region Using Statements
using System;
using System.IO;

using Narivia.Settings;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;

#elif __IOS__ || __TVOS__
using Foundation;
using UIKit;
#endif
#endregion

namespace Narivia
{
    #if __IOS__ || __TVOS__
    [Register("AppDelegate")]
    class Program : UIApplicationDelegate
    
#else
    static class Program
    #endif
    {
        public static GameWindow Game { get; private set; }

        internal static void RunGame()
        {
            Game = new GameWindow();
            Game.Run();
            #if !__IOS__  && !__TVOS__
            Game.Dispose();
            #endif
        }

        internal static void PrepareFiles()
        {
            if(!Directory.Exists(ApplicationPaths.UserDataDirectory))
            {
                Directory.CreateDirectory(ApplicationPaths.UserDataDirectory);
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        #if !MONOMAC && !__IOS__  && !__TVOS__
        [STAThread]
        #endif
        static void Main(string[] args)
        {
            PrepareFiles();

            #if MONOMAC
            NSApplication.Init ();

            using (var p = new NSAutoreleasePool ()) {
                NSApplication.SharedApplication.Delegate = new AppDelegate();
                NSApplication.Main(args);
            }
            #elif __IOS__ || __TVOS__
            UIApplication.Main(args, null, "AppDelegate");
            #else
            RunGame();
            #endif
        }

        #if __IOS__ || __TVOS__
        public override void FinishedLaunching(UIApplication app)
        {
            RunGame();
        }
        #endif
    }

    #if MONOMAC
    class AppDelegate : NSApplicationDelegate
    {
        public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs a) =>  {
                if (a.Name.StartsWith("MonoMac")) {
                    return typeof(MonoMac.AppKit.AppKitFramework).Assembly;
                }
                return null;
            };
            Program.RunGame();
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
        {
            return true;
        }
    }  
    #endif
}

