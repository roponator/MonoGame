// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

//#define ROPO_TIME

using System;
using Microsoft.Xna;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content
{
    public class Texture2DReader : ContentTypeReader<Texture2D>
    {
        internal Texture2DReader()
        {
            // Do nothing
        }

        //  public static System.Diagnostics.Stopwatch stopwatchTexRead = new System.Diagnostics.Stopwatch ();
        // public static long t1 = 0, t2 = 0,tReadContent=0,tProcessContent=0;

        protected internal override Texture2D Read(ContentReader reader, Texture2D existingInstance)
        {
            Texture2D texture = null;

            var surfaceFormat = (SurfaceFormat)reader.ReadInt32();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int levelCount = reader.ReadInt32();
            int levelCountOutput = levelCount;

            // If the system does not fully support Power of Two textures,
            // skip any mip maps supplied with any non PoT textures.
            if (levelCount > 1 && !reader.GraphicsDevice.GraphicsCapabilities.SupportsNonPowerOfTwo &&
                (!MathHelper.IsPowerOfTwo(width) || !MathHelper.IsPowerOfTwo(height)))
            {
                levelCountOutput = 1;
                System.Diagnostics.Debug.WriteLine(  // TODO ROPO REPLACE
                    "Device does not support non Power of Two textures. Skipping mipmaps.");
            }

            SurfaceFormat convertedFormat = surfaceFormat;
            switch (surfaceFormat)
            {
                case SurfaceFormat.Dxt1:
                case SurfaceFormat.Dxt1a:
                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsDxt1)
                        convertedFormat = SurfaceFormat.Color;
                    break;
                case SurfaceFormat.Dxt1SRgb:
                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsDxt1)
                        convertedFormat = SurfaceFormat.ColorSRgb;
                    break;
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc)
                        convertedFormat = SurfaceFormat.Color;
                    break;
                case SurfaceFormat.Dxt3SRgb:
                case SurfaceFormat.Dxt5SRgb:
                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc)
                        convertedFormat = SurfaceFormat.ColorSRgb;
                    break;
                case SurfaceFormat.NormalizedByte4:
                    convertedFormat = SurfaceFormat.Color;
                    break;
            }

            //  stopwatchTexRead.Stop ();
            //  t1 += stopwatchTexRead.ElapsedMilliseconds;



            //  stopwatchTexRead.Reset ();
            //  stopwatchTexRead.Start ();

            texture = existingInstance ?? new Texture2D(reader.GraphicsDevice, width, height, levelCountOutput > 1, convertedFormat);

            //  stopwatchTexRead.Stop ();
            //   t2 += stopwatchTexRead.ElapsedMilliseconds;

#if OPENGL
            Threading.BlockOnUIThread(() =>
            {
#endif


                //System.Diagnostics.Stopwatch internalSw = new System.Diagnostics.Stopwatch ();


                for (int level = 0; level < levelCount; level++)
                {
                    // internalSw.Start ();

                    var levelDataSizeInBytes = reader.ReadInt32();
                    var levelData = reader.ContentManager.GetScratchBuffer(levelDataSizeInBytes);
                    reader.Read(levelData, 0, levelDataSizeInBytes);

                    // internalSw.Stop ();
                    //  tReadContent += internalSw.ElapsedMilliseconds;

                    //  internalSw.Reset ();
                    // internalSw.Start ();

                    int levelWidth = width >> level;
                    int levelHeight = height >> level;

                    if (level >= levelCountOutput)
                        continue;

                    //Convert the image data if required
                    switch (surfaceFormat)
                    {
                        case SurfaceFormat.Dxt1:
                        case SurfaceFormat.Dxt1SRgb:
                        case SurfaceFormat.Dxt1a:
                            if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsDxt1 && convertedFormat == SurfaceFormat.Color)
                            {
                                levelData = DxtUtil.DecompressDxt1(levelData, levelWidth, levelHeight);
                                levelDataSizeInBytes = levelData.Length;
                            }
                            break;
                        case SurfaceFormat.Dxt3:
                        case SurfaceFormat.Dxt3SRgb:
                            if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc)
                                if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc &&
                                    convertedFormat == SurfaceFormat.Color)
                                {
                                    levelData = DxtUtil.DecompressDxt3(levelData, levelWidth, levelHeight);
                                    levelDataSizeInBytes = levelData.Length;
                                }
                            break;
                        case SurfaceFormat.Dxt5:
                        case SurfaceFormat.Dxt5SRgb:
                            if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc)
                                if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc &&
                                    convertedFormat == SurfaceFormat.Color)
                                {
                                    levelData = DxtUtil.DecompressDxt5(levelData, levelWidth, levelHeight);
                                    levelDataSizeInBytes = levelData.Length;
                                }
                            break;
                        case SurfaceFormat.Bgra5551:
                            {
#if OPENGL
                                // Shift the channels to suit OpenGL
                                int offset = 0;
                                for (int y = 0; y < levelHeight; y++)
                                {
                                    for (int x = 0; x < levelWidth; x++)
                                    {
                                        ushort pixel = BitConverter.ToUInt16(levelData, offset);
                                        pixel = (ushort)(((pixel & 0x7FFF) << 1) | ((pixel & 0x8000) >> 15));
                                        levelData[offset] = (byte)(pixel);
                                        levelData[offset + 1] = (byte)(pixel >> 8);
                                        offset += 2;
                                    }
                                }
#endif
                            }
                            break;
                        case SurfaceFormat.Bgra4444:
                            {
#if OPENGL
                                // Shift the channels to suit OpenGL
                                int offset = 0;
                                for (int y = 0; y < levelHeight; y++)
                                {
                                    for (int x = 0; x < levelWidth; x++)
                                    {
                                        ushort pixel = BitConverter.ToUInt16(levelData, offset);
                                        pixel = (ushort)(((pixel & 0x0FFF) << 4) | ((pixel & 0xF000) >> 12));
                                        levelData[offset] = (byte)(pixel);
                                        levelData[offset + 1] = (byte)(pixel >> 8);
                                        offset += 2;
                                    }
                                }
#endif
                            }
                            break;
                        case SurfaceFormat.NormalizedByte4:
                            {
                                int bytesPerPixel = surfaceFormat.GetSize();
                                int pitch = levelWidth * bytesPerPixel;
                                for (int y = 0; y < levelHeight; y++)
                                {
                                    for (int x = 0; x < levelWidth; x++)
                                    {
                                        int color = BitConverter.ToInt32(levelData, y * pitch + x * bytesPerPixel);
                                        levelData[y * pitch + x * 4] = (byte)(((color >> 16) & 0xff)); //R:=W
                                        levelData[y * pitch + x * 4 + 1] = (byte)(((color >> 8) & 0xff)); //G:=V
                                        levelData[y * pitch + x * 4 + 2] = (byte)(((color) & 0xff)); //B:=U
                                        levelData[y * pitch + x * 4 + 3] = (byte)(((color >> 24) & 0xff)); //A:=Q
                                    }
                                }
                            }
                            break;
                    }

                    texture.SetData(level, null, levelData, 0, levelDataSizeInBytes);

                    // internalSw.Stop ();
                    // tProcessContent += internalSw.ElapsedMilliseconds;

                }
#if OPENGL
            });
#endif

            return texture;

        }

        #if ROPO_TASK_TIME_PLOT
        int xx = 0; // todo remove
#endif

        protected internal override void ReadCallback(ContentManager.ResTask task, ContentReader reader, Texture2D existingInstance, ContentManager.ResCallback onDone)
        {
            Texture2D texture = null;

#if ROPO_TIME
            System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
            sw1.Start();
#endif
            var surfaceFormat = (SurfaceFormat)reader.ReadInt32();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int levelCount = reader.ReadInt32();
            int levelCountOutput = levelCount;

            // If the system does not fully support Power of Two textures,
            // skip any mip maps supplied with any non PoT textures.
            if (levelCount > 1 && !reader.GraphicsDevice.GraphicsCapabilities.SupportsNonPowerOfTwo &&
                (!MathHelper.IsPowerOfTwo(width) || !MathHelper.IsPowerOfTwo(height)))
            {
                levelCountOutput = 1;
                System.Diagnostics.Debug.WriteLine(  // TODO ROPO REPLACE
                    "Device does not support non Power of Two textures. Skipping mipmaps.");
            }

            SurfaceFormat convertedFormat = surfaceFormat;
            switch (surfaceFormat)
            {
                case SurfaceFormat.Dxt1:
                case SurfaceFormat.Dxt1a:
                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsDxt1)
                        convertedFormat = SurfaceFormat.Color;
                    break;
                case SurfaceFormat.Dxt1SRgb:
                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsDxt1)
                        convertedFormat = SurfaceFormat.ColorSRgb;
                    break;
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc)
                        convertedFormat = SurfaceFormat.Color;
                    break;
                case SurfaceFormat.Dxt3SRgb:
                case SurfaceFormat.Dxt5SRgb:
                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc)
                        convertedFormat = SurfaceFormat.ColorSRgb;
                    break;
                case SurfaceFormat.NormalizedByte4:
                    convertedFormat = SurfaceFormat.Color;
                    break;
            }


         
#if OPENGL
            // Threading.BlockOnUIThread (() =>
#endif

                    int[] levelDataSizeInBytes = new int[levelCount];
                    byte[][] levelData = new byte[levelCount][];

                  
                    for (int level = 0; level < levelCount; level++)
                    {
                        levelDataSizeInBytes[level] = reader.ReadInt32();

#if ROPO_TASK_TIME_PLOT
                ++xx;  MST TEXTURES ARE <1MB, MAKE POOLS OF DIFFERENT BUFFER SIZES?
            ContentManager.addPlotXYPair("size",xx,levelDataSizeInBytes[level]);
#endif



                // TODO REUSE THIS DATA
                levelData[level] =  new byte[levelDataSizeInBytes[level]]; // reader.ContentManager.GetScratchBuffer(levelDataSizeInBytes[level]);
                        reader.Read(levelData[level], 0, levelDataSizeInBytes[level]);

                        int levelWidth = width >> level;
                        int levelHeight = height >> level;

                        if (level >= levelCountOutput)
                            continue;

                        //Convert the image data if required
                        switch (surfaceFormat)
                        {
                            case SurfaceFormat.Dxt1:
                            case SurfaceFormat.Dxt1SRgb:
                            case SurfaceFormat.Dxt1a:
                                if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsDxt1 && convertedFormat == SurfaceFormat.Color)
                                {
                                    levelData[level] = DxtUtil.DecompressDxt1(levelData[level], levelWidth, levelHeight);
                                    levelDataSizeInBytes[level] = levelData[level].Length;
                                }
                                break;
                            case SurfaceFormat.Dxt3:
                            case SurfaceFormat.Dxt3SRgb:
                                if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc)
                                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc &&
                                        convertedFormat == SurfaceFormat.Color)
                                    {
                                        levelData[level] = DxtUtil.DecompressDxt3(levelData[level], levelWidth, levelHeight);
                                        levelDataSizeInBytes[level] = levelData[level].Length;
                                    }
                                break;
                            case SurfaceFormat.Dxt5:
                            case SurfaceFormat.Dxt5SRgb:
                                if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc)
                                    if (!reader.GraphicsDevice.GraphicsCapabilities.SupportsS3tc &&
                                        convertedFormat == SurfaceFormat.Color)
                                    {
                                        levelData[level] = DxtUtil.DecompressDxt5(levelData[level], levelWidth, levelHeight);
                                        levelDataSizeInBytes[level] = levelData[level].Length;
                                    }
                                break;
                            case SurfaceFormat.Bgra5551:
                                {
#if OPENGL
                                    // Shift the channels to suit OpenGL
                                    int offset = 0;
                                    for (int y = 0; y < levelHeight; y++)
                                    {
                                        for (int x = 0; x < levelWidth; x++)
                                        {
                                            ushort pixel = BitConverter.ToUInt16(levelData[level], offset);
                                            pixel = (ushort)(((pixel & 0x7FFF) << 1) | ((pixel & 0x8000) >> 15));
                                            levelData[level][offset] = (byte)(pixel);
                                            levelData[level][offset + 1] = (byte)(pixel >> 8);
                                            offset += 2;
                                        }
                                    }
#endif
                                }
                                break;
                            case SurfaceFormat.Bgra4444:
                                {
#if OPENGL
                                    // Shift the channels to suit OpenGL
                                    int offset = 0;
                                    for (int y = 0; y < levelHeight; y++)
                                    {
                                        for (int x = 0; x < levelWidth; x++)
                                        {
                                            ushort pixel = BitConverter.ToUInt16(levelData[level], offset);
                                            pixel = (ushort)(((pixel & 0x0FFF) << 4) | ((pixel & 0xF000) >> 12));
                                            levelData[level][offset] = (byte)(pixel);
                                            levelData[level][offset + 1] = (byte)(pixel >> 8);
                                            offset += 2;
                                        }
                                    }
#endif
                                }
                                break;
                            case SurfaceFormat.NormalizedByte4:
                                {
                                    int bytesPerPixel = surfaceFormat.GetSize();
                                    int pitch = levelWidth * bytesPerPixel;
                                    for (int y = 0; y < levelHeight; y++)
                                    {
                                        for (int x = 0; x < levelWidth; x++)
                                        {
                                            int color = BitConverter.ToInt32(levelData[level], y * pitch + x * bytesPerPixel);
                                            levelData[level][y * pitch + x * 4] = (byte)(((color >> 16) & 0xff)); //R:=W
                                            levelData[level][y * pitch + x * 4 + 1] = (byte)(((color >> 8) & 0xff)); //G:=V
                                            levelData[level][y * pitch + x * 4 + 2] = (byte)(((color) & 0xff)); //B:=U
                                            levelData[level][y * pitch + x * 4 + 3] = (byte)(((color >> 24) & 0xff)); //A:=Q
                                        }
                                    }
                                }
                                break;
                        }

                        // Game.Instance.Window.log("ropo","format: " + System.Environment.CurrentManagedThreadId);
                    } // end for


#if ROPO_TIME
            sw1.Stop();
            ContentManager.addTime("Texture2DReader::Read",sw1.ElapsedMilliseconds);

#endif


            ContentManager.ResTask setDataTask = new ContentManager.ResTask(true,(___) =>
            {

                // 250ms for all textures, neglectable
                texture = existingInstance ?? new Texture2D(reader.GraphicsDevice, width, height, levelCountOutput > 1, convertedFormat);

                // 2000ms for entire for loop for all textures. TODO POTENTIAL OPTIMIZE, WOULD REQUIRE THREADED OPENGL...
                for (int level = 0; level < levelCount; ++level)
                        {
                            texture.SetData(level, null, levelData[level], 0, levelDataSizeInBytes[level]);
                        }


                onDone(texture);
                    });

#if ROPO_TASK_TIME_PLOT
            setDataTask.plotTimeTaskName = "Texture2D SetData"; // todo could be slow?
#endif

            ContentManager.EnqueueResourceLoadingTaskOnMainThread(setDataTask);
               
        }

    }
}
