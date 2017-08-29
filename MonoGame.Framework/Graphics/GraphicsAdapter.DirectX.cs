// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SharpDX.Direct3D;
using SharpDX.DXGI;

namespace Microsoft.Xna.Framework.Graphics
{
    partial class GraphicsAdapter
    {
        SharpDX.DXGI.Adapter1 _adapter;

        private static void PlatformInitializeAdapters(out ReadOnlyCollection<GraphicsAdapter> adapters)
        {
            var factory = new SharpDX.DXGI.Factory1();
           
            var adapterCount = factory.GetAdapterCount();
            var adapterList = new List<GraphicsAdapter>(adapterCount);

            Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessage("d3d GraphicsAdapter::PlatformInitializeAdapters: adapter count: " + adapterCount);

            for (var i = 0; i < adapterCount; i++)
            {
                var device = factory.GetAdapter1(i);
            
                if (device != null)
                {
                    string desc = "";
                    if (device.Description.Description != null)
                    {
                        desc += "Description: " + device.Description.Description;
                    }
                    if (device.Description.Description != null)
                    {
                        desc += "Description1: " + device.Description1.Description;
                    }
                    Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessage("device: " + desc);
                }
                else
                {
                    Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessage("device: is null");
                }

                var monitorCount = device.GetOutputCount();
                Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessage("monitor count1: " + monitorCount);

                for (var j = 0; j < monitorCount; j++)
                {
                    var monitor = device.GetOutput(j);

                    if (monitor != null)
                    {
                        string desc = "";
                        if (monitor.Description.DeviceName != null)
                        {
                            desc = monitor.Description.DeviceName + ", attached: " + monitor.Description.IsAttachedToDesktop +
                                ", bounds: " + monitor.Description.DesktopBounds.Top+", "
                                + monitor.Description.DesktopBounds.Left + ", "
                                + monitor.Description.DesktopBounds.Bottom + ", "
                                + monitor.Description.DesktopBounds.Right +
                                ", rot: " + monitor.Description.Rotation.ToString();
                        }
                        Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessage("monitor: " + desc);
                    }
                    else
                    {
                        Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessage("monitor null");
                    }

                    try
                    {
                        var adapter = CreateAdapter(device, monitor);

                        if (adapter != null)
                        {
                            int numDisplayModes = 0;
                            adapter.SupportedDisplayModes.GetEnumerator().Reset(); // TODO POTENTIAL FIX???
                            foreach (var dm in adapter.SupportedDisplayModes)
                            {
                                ++numDisplayModes;
                            }
                            adapter.SupportedDisplayModes.GetEnumerator().Reset();// TODO POTENTIAL FIX???
                            string s = " DisplayMode: " + adapter.CurrentDisplayMode.ToString() +
                                ", Description: " + adapter.Description + ", DeviceName: " + adapter.DeviceName +
                                ", IsDefaultAdapter: " + adapter.IsDefaultAdapter + ", IsWideScreen: " + adapter.IsWideScreen +
                                ", num supp disp modes: " + numDisplayModes;

                            Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessage("CreateAdapter: adapter: " + s);
                        }
                        else
                        {
                            Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessage("CreateAdapter: adapter is null");
                        }

                        adapterList.Add(adapter);
                    }
                    catch (Exception e)
                    {
                        Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessageAndSend("CreateAdapter exception", e);
                    }


                    monitor.Dispose();
                }
            }

            factory.Dispose();

            adapters = new ReadOnlyCollection<GraphicsAdapter>(adapterList);
        }

        private static readonly Dictionary<SharpDX.DXGI.Format, SurfaceFormat> FormatTranslations = new Dictionary<SharpDX.DXGI.Format, SurfaceFormat>
        {
            { SharpDX.DXGI.Format.R8G8B8A8_UNorm, SurfaceFormat.Color },
            { SharpDX.DXGI.Format.B8G8R8A8_UNorm, SurfaceFormat.Color },
            { SharpDX.DXGI.Format.B5G6R5_UNorm, SurfaceFormat.Bgr565 },
        };

        private static GraphicsAdapter CreateAdapter(SharpDX.DXGI.Adapter1 device, SharpDX.DXGI.Output monitor)
        {
            var adapter = new GraphicsAdapter();
            adapter._adapter = device;

            adapter.DeviceName = monitor.Description.DeviceName.TrimEnd(new char[] { '\0' });
            adapter.Description = device.Description1.Description.TrimEnd(new char[] { '\0' });
            adapter.DeviceId = device.Description1.DeviceId;
            adapter.Revision = device.Description1.Revision;
            adapter.VendorId = device.Description1.VendorId;
            adapter.SubSystemId = device.Description1.SubsystemId;
            adapter.MonitorHandle = monitor.Description.MonitorHandle;

#if WINDOWS_UAP
            var desktopWidth = monitor.Description.DesktopBounds.Right - monitor.Description.DesktopBounds.Left;
            var desktopHeight = monitor.Description.DesktopBounds.Bottom - monitor.Description.DesktopBounds.Top;
#else
            var desktopWidth = monitor.Description.DesktopBounds.Width;
            var desktopHeight = monitor.Description.DesktopBounds.Height;
#endif

            var modes = new List<DisplayMode>();

            foreach (var formatTranslation in FormatTranslations)
            {
                SharpDX.DXGI.ModeDescription[] displayModes;

                // This can fail on headless machines, so just assume the desktop size
                // is a valid mode and return that... so at least our unit tests work.
                try
                {
                    displayModes = monitor.GetDisplayModeList(formatTranslation.Key, 0);
                }
                catch (SharpDX.SharpDXException e)
                {
                    var mode = new DisplayMode(desktopWidth, desktopHeight, SurfaceFormat.Color);
                    modes.Add(mode);
                    adapter._currentDisplayMode = mode;
                    Microsoft.Xna.Framework.Graphics.GraphicsDevice.RopoAddMessage("GraphicsAdapter.DirectX.cs CreateAdapter() EXCEPTION potential non-fatal: " + e.ToString() + e.StackTrace.ToString());
                    break;
                }


                foreach (var displayMode in displayModes)
                {
                    var mode = new DisplayMode(displayMode.Width, displayMode.Height, formatTranslation.Value);

                    // Skip duplicate modes with the same width/height/formats.
                    if (modes.Contains(mode))
                        continue;

                    modes.Add(mode);

                    if (adapter._currentDisplayMode == null)
                    {
                        if (mode.Width == desktopWidth && mode.Height == desktopHeight && mode.Format == SurfaceFormat.Color)
                            adapter._currentDisplayMode = mode;
                    }
                }
            }

            adapter._supportedDisplayModes = new DisplayModeCollection(modes);

            if (adapter._currentDisplayMode == null) //(i.e. desktop mode wasn't found in the available modes)
                adapter._currentDisplayMode = new DisplayMode(desktopWidth, desktopHeight, SurfaceFormat.Color);

            return adapter;
        }

        private bool PlatformIsProfileSupported(GraphicsProfile graphicsProfile)
        {
            if (UseReferenceDevice)
                return true;

            if (_adapter == null)
            {
                Graphics.GraphicsDevice.RopoAddMessage("GraphicsAdapter.DirectX.cs PlatformIsProfileSupported: null adapter"); 
            }
    
            switch (graphicsProfile)
            {
                case GraphicsProfile.Reach:
                    return SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(_adapter, FeatureLevel.Level_9_1);
                case GraphicsProfile.HiDef:
                    return SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(_adapter, FeatureLevel.Level_10_0);
                default:
                    throw new InvalidOperationException();
            }

          
        }
    }
}
