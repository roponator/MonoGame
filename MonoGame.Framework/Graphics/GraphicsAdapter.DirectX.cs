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

        private static readonly Dictionary<SharpDX.DXGI.Format, SurfaceFormat> FormatTranslations = new Dictionary<SharpDX.DXGI.Format, SurfaceFormat>
        {
            { SharpDX.DXGI.Format.R8G8B8A8_UNorm, SurfaceFormat.Color },
            { SharpDX.DXGI.Format.B8G8R8A8_UNorm, SurfaceFormat.Color },
            { SharpDX.DXGI.Format.B5G6R5_UNorm, SurfaceFormat.Bgr565 },
        };

        private static void PlatformInitializeAdapters11_Vanilla(out ReadOnlyCollection<GraphicsAdapter> adapters)
        {
            const string NAME = "PlatformInitializeAdapters11_Vanilla";
            // TODO ROPO: COULD SEND HIM A DEBUG FACTORY BUILD, NOT SURE IF HE CAN RUN IT, AS IF YOU NEED SOME SPECIAL DEBUG SDK LAYER...
            bool debugFactory = false;
            Factory2 factory = new SharpDX.DXGI.Factory2(debugFactory);

            logToFileBlocking("GraphicsAdapter::"+ NAME+" 1: factory: " + (factory != null));

            if(factory != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + " 1_1: factory type: " + (factory.GetType().Name));
            }

            var adapterCount = factory.GetAdapterCount1();
            logToFileBlocking("GraphicsAdapter::" + NAME + "  2.1: adapter count: " + factory.GetAdapterCount());
            logToFileBlocking("GraphicsAdapter::" + NAME + "  2.2: adapter1 count: " + factory.GetAdapterCount1());

            var adapterList = new List<GraphicsAdapter>(adapterCount);
            logToFileBlocking("GraphicsAdapter::" + NAME + "  3: adapter list: " + (adapterList != null));
            if(adapterList != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  4: adapter count: " + adapterList.Count);
            }

            for (var i = 0; i < adapterCount; i++)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  5: Adapter Loop: : " + i);

                Adapter1 device = factory.GetAdapter1(i);
                logToFileBlocking("GraphicsAdapter::" + NAME + "  6: Adapter Loop: device: " + (device != null));

                if (device != null)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + " 6_1: Adapter type: " + device.GetType().Name);
                }

                var monitorCount = device.GetOutputCount();
                for (var j = 0; j < monitorCount; j++)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  7: Adapter Loop: Monitor loop 1: " + j);

                    // 'Output1' derives from 'Output' so this can also return Output1 if implemented so.
                    Output monitor = device.GetOutput(j);
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  8: Adapter Loop: Monitor loop 2: " + (monitor!=null));

                    var adapter = CreateAdapter11_Vanilla(device, monitor);
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  9: Adapter Loop: Monitor loop 3: " + (adapter != null));

                    adapterList.Add(adapter);

                    monitor.Dispose();
                }
            }

            factory.Dispose();

            adapters = new ReadOnlyCollection<GraphicsAdapter>(adapterList);

            logToFileBlocking("GraphicsAdapter::" + NAME + "  10: adapters: " + (adapters != null));
            if(adapters != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  11: adapters count: " + (adapters.Count));

                for(int i=0;i<adapters.Count;++i)
                {
                     logToFileBlocking("GraphicsAdapter::" + NAME + "  11: adapters count: " + (adapters[i] != null));
                }
            }
           
        }

       /* private static void PlatformInitializeAdapters11_DisplayModes1(out ReadOnlyCollection<GraphicsAdapter> adapters)
        {
            const string NAME = "PlatformInitializeAdapters11_DisplayModes1";

            var factory = new SharpDX.DXGI.Factory1();

            logToFileBlocking("GraphicsAdapter::" + NAME + " 1: factory: " + (factory != null));

            if (factory != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + " 1_1: factory type: " + (factory.GetType().Name));
            }

            var adapterCount = factory.GetAdapterCount1();
            logToFileBlocking("GraphicsAdapter::" + NAME + "  2.1: adapter count: " + factory.GetAdapterCount());
            logToFileBlocking("GraphicsAdapter::" + NAME + "  2.2: adapter1 count: " + factory.GetAdapterCount1());

            var adapterList = new List<GraphicsAdapter>(adapterCount);
            logToFileBlocking("GraphicsAdapter::" + NAME + "  3: adapter list: " + (adapterList != null));
            if (adapterList != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  4: adapter count: " + adapterList.Count);
            }

            for (var i = 0; i < adapterCount; i++)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  5: Adapter Loop: : " + i);

                Adapter1 device = factory.GetAdapter1(i);
                logToFileBlocking("GraphicsAdapter::" + NAME + "  6: Adapter Loop: device: " + (device != null));

                if (device != null)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  Adapter type: " + device.GetType().Name);
                }

                var monitorCount = device.GetOutputCount();
                for (var j = 0; j < monitorCount; j++)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  7: Adapter Loop: Monitor loop 1: " + j);

                    // 'Output1' derives from 'Output' so this can also return Output1 if implemented so.
                    Output monitor = device.GetOutput(j);
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  8: Adapter Loop: Monitor loop 2: " + (monitor != null));

                    var adapter = CreateAdapter11_DisplayModes1(device, monitor);
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  9: Adapter Loop: Monitor loop 3: " + (adapter != null));

                    adapterList.Add(adapter);

                    monitor.Dispose();
                }
            }

            factory.Dispose();

            adapters = new ReadOnlyCollection<GraphicsAdapter>(adapterList);

            logToFileBlocking("GraphicsAdapter::" + NAME + "  10: adapters: " + (adapters != null));
            if (adapters != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  11: adapters count: " + (adapters.Count));

                for (int i = 0; i < adapters.Count; ++i)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  11: adapters count: " + (adapters[i] != null));
                }
            }

        }*/

        private static void PlatformInitializeAdapters12(out ReadOnlyCollection<GraphicsAdapter> adapters)
        {
            const string NAME = "PlatformInitializeAdapters12";

            Factory2 factory = new SharpDX.DXGI.Factory2();

            logToFileBlocking("GraphicsAdapter::" + NAME + " 1: factory: " + (factory != null));

            if (factory != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + " 1_1: factory type: " + (factory.GetType().Name));
            }

            var adapterCount = factory.GetAdapterCount1();
            logToFileBlocking("GraphicsAdapter::" + NAME + "  2.1: adapter count: " + factory.GetAdapterCount());
            logToFileBlocking("GraphicsAdapter::" + NAME + "  2.2: adapter1 count: " + factory.GetAdapterCount1());

            var adapterList = new List<GraphicsAdapter>(adapterCount);
            logToFileBlocking("GraphicsAdapter::" + NAME + "  3: adapter list: " + (adapterList != null));
            if (adapterList != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  4: adapter count: " + adapterList.Count);
            }

            for (var i = 0; i < adapterCount; i++)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  5: Adapter Loop: : " + i);

                Adapter1 device = factory.GetAdapter1(i);
                logToFileBlocking("GraphicsAdapter::" + NAME + "  6: Adapter Loop: device: " + (device != null));

                if (device != null)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  Adapter type: " + device.GetType().Name);
                }

                var monitorCount = device.GetOutputCount();
                for (var j = 0; j < monitorCount; j++)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  7: Adapter Loop: Monitor loop 1: " + j);

                    // 'Output1' derives from 'Output' so this can also return Output1 if implemented so.
                    Output monitor = device.GetOutput(j);
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  8: Adapter Loop: Monitor loop 2: " + (monitor != null));

                    Adapter2 ad2 = (Adapter2)device;
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  8: Adapter Loop: Monitor loop 2_1: " + (monitor != null));

                    var adapter = CreateAdapter12(ad2, monitor);
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  9: Adapter Loop: Monitor loop 3: " + (adapter != null));

                    adapterList.Add(adapter);

                    monitor.Dispose();
                }
            }

            factory.Dispose();

            adapters = new ReadOnlyCollection<GraphicsAdapter>(adapterList);

            logToFileBlocking("GraphicsAdapter::" + NAME + "  10: adapters: " + (adapters != null));
            if (adapters != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  11: adapters count: " + (adapters.Count));

                for (int i = 0; i < adapters.Count; ++i)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  11: adapters count: " + (adapters[i] != null));
                }
            }

        }

        private static GraphicsAdapter CreateAdapter11_Vanilla(SharpDX.DXGI.Adapter1 device, SharpDX.DXGI.Output monitor)
        {
            const string NAME = "CreateAdapter11";

            logToFileBlocking("GraphicsAdapter::" + NAME + "  1: " + (device != null) + ", " + (monitor != null));

            if (monitor != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  1_1 monitor type: " + monitor.GetType().Name);
            }

            var adapter = new GraphicsAdapter();
            adapter._adapter = device;

            logToFileBlocking("GraphicsAdapter::" + NAME + "  1_2: " + (adapter != null));

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
            logToFileBlocking("GraphicsAdapter::" + NAME + "  2");

            foreach (var formatTranslation in FormatTranslations)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1");

                SharpDX.DXGI.ModeDescription[] displayModes;

                // This can fail on headless machines, so just assume the desktop size
                // is a valid mode and return that... so at least our unit tests work.
                try
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_1");

                    displayModes = monitor.GetDisplayModeList(formatTranslation.Key, 0);

                    if (displayModes != null && displayModes.Length > 0)
                    {
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_2 type: " + displayModes[0].GetType().Name);
                    }

                  /*  try
                    {
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3_1: displayModeLoop 1_1_1: try start");
                        Output1 mon1 = (Output1)monitor;
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3_1: displayModeLoop 1_1_2: try start");
                        ModeDescription1[] modes1 = mon1.GetDisplayModeList1(formatTranslation.Key, 0);
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3_1: displayModeLoop 1_1_2: modes1 len: "+ modes1.Length);
                        if(modes1.Length>0)
                        {
                            logToFileBlocking("GraphicsAdapter::" + NAME + "  3_1: displayModeLoop 1_1_2: modes1.width: " + modes1[0].Width);
                        }
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3_1: displayModeLoop 1_1_3: try end");

                    }
                    catch (Exception e)
                    {
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3_1: displayModeLoop 1_1_1_fail: exception: "+e.ToString());
                    }*/

                }
                catch (SharpDX.SharpDXException)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_3");

                    var mode = new DisplayMode(desktopWidth, desktopHeight, SurfaceFormat.Color);

                    modes.Add(mode);
                    adapter._currentDisplayMode = mode;
                    break;
                }

                logToFileBlocking("GraphicsAdapter::" + NAME + "  4: displayModeLoop 2: disp mode:  " + (displayModes != null));

                if (displayModes != null)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  5: displayModeLoop 3 num disp modes: " + displayModes.Length);
                }

                foreach (var displayMode in displayModes)
                {
                    var mode = new DisplayMode(displayMode.Width, displayMode.Height, formatTranslation.Value);
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  6: displayModeLoop 4  mode: " + (mode != null));

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

            logToFileBlocking("GraphicsAdapter::" + NAME + "  7: " + (modes != null));

            if (modes != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  7: " + (modes.Count));
            }

            adapter._supportedDisplayModes = new DisplayModeCollection(modes);

            if (adapter._currentDisplayMode == null) //(i.e. desktop mode wasn't found in the available modes)
                adapter._currentDisplayMode = new DisplayMode(desktopWidth, desktopHeight, SurfaceFormat.Color);

            logToFileBlocking("GraphicsAdapter::" + NAME + "  8: adapter: " + (adapter != null));

            return adapter;
        }

        private static GraphicsAdapter CreateAdapter11_DisplayModes1(SharpDX.DXGI.Adapter1 device, SharpDX.DXGI.Output monitor)
        {
            const string NAME = "CreateAdapter11_DisplayModes1";

            logToFileBlocking("GraphicsAdapter::" + NAME + "  1: " + (device != null) + ", " + (monitor != null));

            if (monitor != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  1_1 monitor type: " + monitor.GetType().Name);
            }

            var adapter = new GraphicsAdapter();
            adapter._adapter = device;

            logToFileBlocking("GraphicsAdapter::" + NAME + "  1_2: " + (adapter != null));

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
            logToFileBlocking("GraphicsAdapter::" + NAME + "  2");

            foreach (var formatTranslation in FormatTranslations)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1");

                SharpDX.DXGI.ModeDescription[] displayModes;

                // This can fail on headless machines, so just assume the desktop size
                // is a valid mode and return that... so at least our unit tests work.
                try
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_1");

                    displayModes = (monitor).GetDisplayModeList(formatTranslation.Key, 0);

                    if (displayModes != null && displayModes.Length > 0)
                    {
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_2 type: " + displayModes[0].GetType().Name);
                    }
                }
                catch (SharpDX.SharpDXException)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_3");

                    var mode = new DisplayMode(desktopWidth, desktopHeight, SurfaceFormat.Color);

                    modes.Add(mode);
                    adapter._currentDisplayMode = mode;
                    break;
                }

                logToFileBlocking("GraphicsAdapter::" + NAME + "  4: displayModeLoop 2: disp mode:  " + (displayModes != null));

                if (displayModes != null)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  5: displayModeLoop 3 num disp modes: " + displayModes.Length);
                }

                foreach (var displayMode in displayModes)
                {
                    var mode = new DisplayMode(displayMode.Width, displayMode.Height, formatTranslation.Value);
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  6: displayModeLoop 4  mode: " + (mode != null));

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

            logToFileBlocking("GraphicsAdapter::" + NAME + "  7: " + (modes != null));

            if (modes != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  7: " + (modes.Count));
            }

            adapter._supportedDisplayModes = new DisplayModeCollection(modes);

            if (adapter._currentDisplayMode == null) //(i.e. desktop mode wasn't found in the available modes)
                adapter._currentDisplayMode = new DisplayMode(desktopWidth, desktopHeight, SurfaceFormat.Color);

            logToFileBlocking("GraphicsAdapter::" + NAME + "  8: adapter: " + (adapter != null));

            return adapter;
        }

        private static GraphicsAdapter CreateAdapter12(SharpDX.DXGI.Adapter2 device, SharpDX.DXGI.Output monitor)
        {
            const string NAME = "CreateAdapter12";

            logToFileBlocking("GraphicsAdapter::" + NAME + "  1: " + (device != null) + ", " + (monitor != null));

            if (monitor != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  1_1 monitor type: " + monitor.GetType().Name);
            }

            var adapter2Def = new GraphicsAdapter();
            adapter2Def._adapter = device;

            logToFileBlocking("GraphicsAdapter::" + NAME + "  1_2: " + (adapter2Def != null));

            adapter2Def.DeviceName = monitor.Description.DeviceName.TrimEnd(new char[] { '\0' });
            adapter2Def.Description = device.Description2.Description.TrimEnd(new char[] { '\0' });
            adapter2Def.DeviceId = device.Description2.DeviceId;
            adapter2Def.Revision = device.Description2.Revision;
            adapter2Def.VendorId = device.Description2.VendorId;
            adapter2Def.SubSystemId = device.Description2.SubsystemId;
            adapter2Def.MonitorHandle = monitor.Description.MonitorHandle;

#if WINDOWS_UAP
            var desktopWidth = monitor.Description.DesktopBounds.Right - monitor.Description.DesktopBounds.Left;
            var desktopHeight = monitor.Description.DesktopBounds.Bottom - monitor.Description.DesktopBounds.Top;
#else
            var desktopWidth = monitor.Description.DesktopBounds.Width;
            var desktopHeight = monitor.Description.DesktopBounds.Height;
#endif

            var modes = new List<DisplayMode>();
            logToFileBlocking("GraphicsAdapter::" + NAME + "  2");

            foreach (var formatTranslation in FormatTranslations)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1");

                SharpDX.DXGI.ModeDescription1[] displayModes;

                // This can fail on headless machines, so just assume the desktop size
                // is a valid mode and return that... so at least our unit tests work.
                try
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_1");

                    displayModes = ((Output2)monitor).GetDisplayModeList1(formatTranslation.Key, 0);

                    if (displayModes != null)
                    {
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_2_1 count: " + displayModes.Length);
                    }

                    if (displayModes != null && displayModes.Length > 0)
                    {
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_2 type: " + displayModes[0].GetType().Name);
                    }

                    SharpDX.DXGI.ModeDescription[] tmpDispModes = ((Output2)monitor).GetDisplayModeList(formatTranslation.Key, 0);

                    if (tmpDispModes != null)
                    {
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_3_1 count: " + tmpDispModes.Length);
                    }

                    if (tmpDispModes != null && tmpDispModes.Length > 0)
                    {
                        logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_4 type: " + tmpDispModes[0].GetType().Name);
                    }
                }
                catch (SharpDX.SharpDXException)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  3: displayModeLoop 1_3");

                    var mode = new DisplayMode(desktopWidth, desktopHeight, SurfaceFormat.Color);

                    modes.Add(mode);
                    adapter2Def._currentDisplayMode = mode;
                    break;
                }

                logToFileBlocking("GraphicsAdapter::" + NAME + "  4: displayModeLoop 2: disp mode:  " + (displayModes != null));

                if (displayModes != null)
                {
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  5: displayModeLoop 3 num disp modes: " + displayModes.Length);
                }

                foreach (var displayMode in displayModes)
                {
                    var mode = new DisplayMode(displayMode.Width, displayMode.Height, formatTranslation.Value);
                    logToFileBlocking("GraphicsAdapter::" + NAME + "  6: displayModeLoop 4  mode: " + (mode != null));

                    // Skip duplicate modes with the same width/height/formats.
                    if (modes.Contains(mode))
                        continue;

                    modes.Add(mode);

                    if (adapter2Def._currentDisplayMode == null)
                    {
                        if (mode.Width == desktopWidth && mode.Height == desktopHeight && mode.Format == SurfaceFormat.Color)
                            adapter2Def._currentDisplayMode = mode;
                    }
                }
            }

            logToFileBlocking("GraphicsAdapter::" + NAME + "  7: " + (modes != null));

            if (modes != null)
            {
                logToFileBlocking("GraphicsAdapter::" + NAME + "  7: " + (modes.Count));
            }

            adapter2Def._supportedDisplayModes = new DisplayModeCollection(modes);

            if (adapter2Def._currentDisplayMode == null) //(i.e. desktop mode wasn't found in the available modes)
                adapter2Def._currentDisplayMode = new DisplayMode(desktopWidth, desktopHeight, SurfaceFormat.Color);

            logToFileBlocking("GraphicsAdapter::" + NAME + "  8: adapter: " + (adapter2Def != null));

            return adapter2Def;
        }


        private bool PlatformIsProfileSupported(GraphicsProfile graphicsProfile)
        {
            logToFileBlocking("GraphicsAdapterDX::PlatformIsProfileSupported 1");

            if (UseReferenceDevice)
                return true;

            logToFileBlocking("GraphicsAdapterDX::PlatformIsProfileSupported 2");


            switch (graphicsProfile)
            {
                case GraphicsProfile.Reach:
                    {
                        logToFileBlocking("GraphicsAdapterDX::PlatformIsProfileSupported Level_9_1");

                        return SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(_adapter, FeatureLevel.Level_9_1);
                    }
                case GraphicsProfile.HiDef:
                    {
                        logToFileBlocking("GraphicsAdapterDX::PlatformIsProfileSupported Level_10_0");

                        return SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(_adapter, FeatureLevel.Level_10_0);
                    }
                default:
                    {
                        logToFileBlocking("GraphicsAdapterDX::PlatformIsProfileSupported Exception");

                        throw new InvalidOperationException();
                    }
            }
        }
    }
}
