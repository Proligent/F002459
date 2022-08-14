using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace F002459.Common
{
    internal partial class USBEnumerator
    {
        #region Variable

        public delegate void UpdateUSBDetectionLogEvent(string information);
        public UpdateUSBDetectionLogEvent UpdateUSBDetectionLogCallback = null;

        private void UpdateUSBDetectionLog(string information)
        {
            if (UpdateUSBDetectionLogCallback != null)
            {
                UpdateUSBDetectionLogCallback(information);
            }
        }

        public const Int32 INVALID_HANDLE_VALUE = -1;

        List<string> PORT_KEY = new List<string>();

        bool PORT_KEY_CASE_SENS = false;

        #endregion

        #region Properties
        /// <summary>
        /// provide the keyword that is used to determine if a port is Download port or not. 
        /// </summary>
        /// <returns></returns>
        public List<string> PortKeyWord
        {
            get { return PORT_KEY; }
            set { PORT_KEY = value; }
        }

        public bool PortKeyWordCaseSensitive
        {
            get { return PORT_KEY_CASE_SENS; }
            set { PORT_KEY_CASE_SENS = value; }
        }
        #endregion

        #region Ger Windows Version
        [StructLayout(LayoutKind.Sequential)]
        public class OSVersionInfo
        {
            public int OSVersionInfoSize;
            public int MajorVersion;
            public int MinorVersion;
            public int BuildNumber;
            public int PlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public String versionString;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetVersionEx([In, Out] OSVersionInfo osvi);

        #endregion

        #region HID_Define

        public const string REGSTR_KEY_USB = "USB";

        public const uint SPDRP_DEVICEDESC = 0x00000000; // DeviceDesc (R/W)
        public const uint SPDRP_HARDWAREID = 0x00000001; // HardwareID (R/W)
        public const uint SPDRP_COMPATIBLEIDS = 0x00000002; // CompatibleIDs (R/W)
        public const uint SPDRP_UNUSED0 = 0x00000003; // unused
        public const uint SPDRP_SERVICE = 0x00000004; // Service (R/W)
        public const uint SPDRP_UNUSED1 = 0x00000005; // unused
        public const uint SPDRP_UNUSED2 = 0x00000006; // unused
        public const uint SPDRP_CLASS = 0x00000007; // Class (R--tied to ClassGUID)
        public const uint SPDRP_CLASSGUID = 0x00000008; // ClassGUID (R/W)
        public const uint SPDRP_DRIVER = 0x00000009; // Driver (R/W)
        public const uint SPDRP_CONFIGFLAGS = 0x0000000A; // ConfigFlags (R/W)
        public const uint SPDRP_MFG = 0x0000000B; // Mfg (R/W)
        public const uint SPDRP_FRIENDLYNAME = 0x0000000C; // FriendlyName (R/W)
        public const uint SPDRP_LOCATION_INFORMATION = 0x0000000D; // LocationInformation (R/W)
        public const uint SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E; // PhysicalDeviceObjectName (R)
        public const uint SPDRP_CAPABILITIES = 0x0000000F; // Capabilities (R)
        public const uint SPDRP_UI_NUMBER = 0x00000010; // UiNumber (R)
        public const uint SPDRP_UPPERFILTERS = 0x00000011; // UpperFilters (R/W)
        public const uint SPDRP_LOWERFILTERS = 0x00000012; // LowerFilters (R/W)
        public const uint SPDRP_BUSTYPEGUID = 0x00000013; // BusTypeGUID (R)
        public const uint SPDRP_LEGACYBUSTYPE = 0x00000014; // LegacyBusType (R)
        public const uint SPDRP_BUSNUMBER = 0x00000015; // BusNumber (R)
        public const uint SPDRP_ENUMERATOR_NAME = 0x00000016; // Enumerator Name (R)
        public const uint SPDRP_SECURITY = 0x00000017; // Security (R/W, binary form)
        public const uint SPDRP_SECURITY_SDS = 0x00000018; // Security (W, SDS form)
        public const uint SPDRP_DEVTYPE = 0x00000019; // Device Type (R/W)
        public const uint SPDRP_EXCLUSIVE = 0x0000001A; // Device is exclusive-access (R/W)
        public const uint SPDRP_CHARACTERISTICS = 0x0000001B; // Device Characteristics (R/W)
        public const uint SPDRP_ADDRESS = 0x0000001C; // Device Address (R)
        public const uint SPDRP_UI_NUMBER_DESC_FORMAT = 0X0000001D; // UiNumberDescFormat (R/W)
        public const uint SPDRP_DEVICE_POWER_DATA = 0x0000001E; // Device Power Data (R)
        public const uint SPDRP_REMOVAL_POLICY = 0x0000001F; // Removal Policy (R)
        public const uint SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020; // Hardware Removal Policy (R)
        public const uint SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021; // Removal Policy Override (RW)
        public const uint SPDRP_INSTALL_STATE = 0x00000022; // Device Install State (R)
        public const uint SPDRP_LOCATION_PATHS = 0x00000023; // Device Location Paths (R)
        public const uint SPDRP_BASE_CONTAINERID = 0x00000024;  // Base ContainerID (R)

        public const uint DIGCF_DEFAULT = 0x00000001;  // only valid with DIGCF_DEVICEINTERFACE
        public const uint DIGCF_PRESENT = 0x00000002;
        public const uint DIGCF_ALLCLASSES = 0x00000004;
        public const uint DIGCF_PROFILE = 0x00000008;
        public const uint DIGCF_DEVICEINTERFACE = 0x00000010;

        public const int BUFFER_SIZE = 2048;

        public const uint REG_SZ = 1;

        #endregion

        #region Kernal_DLL_Import

        internal const UInt32 GENERIC_WRITE = 0x40000000;
        internal const UInt32 FILE_SHARE_WRITE = 0x00000002;
        internal const UInt32 OPEN_EXISTING = 3;
        internal const Int32 MAXIMUM_USB_STRING_LENGTH = 255;
        internal const Int32 IOCTL_USB_GET_NODE_INFORMATION = 0x220408;
        internal const Int32 IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX = 0x220448;
        internal const Int32 IOCTL_USB_GET_NODE_CONNECTION_DRIVERKEY_NAME = 0x220420;

        #region USB_NODE_INFORMATION
        internal enum USB_HUB_NODE : uint
        {
            UsbHub,
            UsbMIParent
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct USB_HUB_DESCRIPTOR
        {
            public Byte bDescriptorLength;
            public Byte bDescriptorType;
            public Byte bNumberOfPorts;
            public Int16 wHubCharacteristics;
            public Byte bPowerOnToPowerGood;
            public Byte bHubControlCurrent;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public Byte[] bRemoveAndPowerMask;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct USB_HUB_INFORMATION
        {
            public USB_HUB_DESCRIPTOR HubDescriptor;
            public Byte HubIsBusPowered;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct USB_MI_PARENT_INFORMATION
        {
            public Int32 NumberOfInterfaces;
        };

        [StructLayout(LayoutKind.Explicit)]
        internal struct UsbNodeUnion
        {
            [FieldOffset(0)]
            public USB_HUB_INFORMATION HubInformation;
            [FieldOffset(0)]
            public USB_MI_PARENT_INFORMATION MiParentInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct USB_NODE_INFORMATION
        {
            public USB_HUB_NODE NodeType;
            public UsbNodeUnion u;
        }
        #endregion

        #region USB_NODE_CONNECTION_INFORMATION_EX
        internal enum USB_CONNECTION_STATUS
        {
            NoDeviceConnected,
            DeviceConnected,
            DeviceFailedEnumeration,
            DeviceGeneralFailure,
            DeviceCausedOvercurrent,
            DeviceNotEnoughPower,
            DeviceNotEnoughBandwidth,
            DeviceHubNestedTooDeeply,
            DeviceInLegacyHub
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct USB_DEVICE_DESCRIPTOR
        {
            public Byte bLength;
            public Byte bDescriptorType;
            public UInt16 bcdUSB;
            public Byte bDeviceClass;
            public Byte bDeviceSubClass;
            public Byte bDeviceProtocol;
            public Byte bMaxPacketSize0;
            public UInt16 idVendor;
            public UInt16 idProduct;
            public UInt16 bcdDevice;
            public Byte iManufacturer;
            public Byte iProduct;
            public Byte iSerialNumber;
            public Byte bNumConfigurations;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct USB_ENDPOINT_DESCRIPTOR
        {
            public Byte bLength;
            public Byte bDescriptorType;
            public Byte bEndpointAddress;
            public Byte bmAttributes;
            public UInt16 wMaxPacketSize;
            public Byte bInterval;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct USB_PIPE_INFO
        {
            public USB_ENDPOINT_DESCRIPTOR EndpointDescriptor;
            public UInt32 ScheduleOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct USB_NODE_CONNECTION_INFORMATION_EX
        {
            public Int32 ConnectionIndex;
            public USB_DEVICE_DESCRIPTOR DeviceDescriptor;
            public Byte CurrentConfigurationValue;
            public Byte Speed;
            public Byte DeviceIsHub;
            public Int16 DeviceAddress;
            public Int32 NumberOfOpenPipes;
            public USB_CONNECTION_STATUS ConnectionStatus;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public USB_PIPE_INFO[] PipeList;
        }
        #endregion

        #region USB_NODE_CONNECTION_DRIVERKEY_NAME
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct USB_NODE_CONNECTION_DRIVERKEY_NAME
        {
            public int ConnectionIndex;
            public int ActualLength;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXIMUM_USB_STRING_LENGTH)]
            public string DriverKeyName;
        }
        #endregion

        #region DeviceIoControl api
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern Boolean DeviceIoControl(
            IntPtr hFile,
            Int32 dwIoControlCode,
            ref USB_NODE_INFORMATION lpInBuffer,
            Int32 nInBufferSize,
            ref USB_NODE_INFORMATION lpOutBuffer,
            Int32 nOutBufferSize,
            out Int32 lpBytesReturned,
            IntPtr lpOverlapped
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern Boolean DeviceIoControl(
            IntPtr hFile,
            Int32 dwIoControlCode,
            ref USB_NODE_CONNECTION_INFORMATION_EX lpInBuffer,
            Int32 nInBufferSize,
            ref USB_NODE_CONNECTION_INFORMATION_EX lpOutBuffer,
            Int32 nOutBufferSize,
            out Int32 lpBytesReturned,
            IntPtr lpOverlapped
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern Boolean DeviceIoControl(
            IntPtr hFile,
            Int32 dwIoControlCode,
            ref USB_NODE_CONNECTION_DRIVERKEY_NAME lpInBuffer,
            Int32 nInBufferSize,
            ref USB_NODE_CONNECTION_DRIVERKEY_NAME lpOutBuffer,
            Int32 nOutBufferSize,
            out Int32 lpBytesReturned,
            IntPtr lpOverlapped
            );
        #endregion

        #region FileIO api
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CreateFile(
             [MarshalAs(UnmanagedType.LPTStr)] string filename,
             [MarshalAs(UnmanagedType.U4)] UInt32 access,
             [MarshalAs(UnmanagedType.U4)] UInt32 share,
             IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
             [MarshalAs(UnmanagedType.U4)] UInt32 creationDisposition,
             [MarshalAs(UnmanagedType.U4)] UInt32 flagsAndAttributes,
             IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hHandle);
        #endregion

        #endregion

        #region SteupApi_DLL_Import

        public const Int32 CR_SUCCESS = 0;
        public const Int32 CM_DRP_CLASSGUID = 0x00000009;
        public const Int32 CM_DRP_DRIVER = 0x0000000A;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVINFO_DATA
        {
            public UInt32 cbSize;
            public Guid ClassGuid;
            public Int32 DevInst;
            public IntPtr Reserved;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, int Enumerator, IntPtr hwndParent, uint Flags); // 1st form using a ClassGUID

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiGetClassDevs(int ClassGuid, string Enumerator, IntPtr hwndParent, uint Flags); // 2nd form uses an Enumerator

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            UInt32 Property,
            ref UInt32 PropertyRegDataType,
            IntPtr PropertyBuffer,
            UInt32 PropertyBufferSize,
            ref UInt32 RequiredSize
            );

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern int CM_Get_Parent(
           ref Int32 pdnDevInst,
           Int32 dnDevInst,
           Int32 ulFlags
        );

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int CM_Get_Device_ID(
           Int32 dnDevInst,
           IntPtr Buffer,
           Int32 BufferLen,
           Int32 ulFlags
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int CM_Get_DevNode_Registry_Property(
            Int32 dnDevInst,
            Int32 ulProperty,
            out Microsoft.Win32.RegistryValueKind pulRegDataType,
            IntPtr Buffer,
            ref UInt32 pulLength,
            Int32 ulFlags);
        #endregion

        #region Guid_Define

        public const string GUID_ADB = "{3f966bd9-fa04-4ec5-991c-d326973b5128}"; //ADB or fastboot devices
        public const string GUID_PORT = "{4D36E978-E325-11CE-BFC1-08002BE10318}"; //Serial and parallel ports
        public const string GUID_HUB = "{36fc9e60-c465-11cf-8056-444553540000}"; //Hub & Composite device. This class includes system-supplied (bus) drivers of USB host controllers and drivers of USB hubs, but not drivers of USB peripherals.
        public const string GUID_HUB_DRIVER = "{f18a0e88-c30c-11d0-8815-00a0c906bed8}";

        #endregion

        #region Enumerator

        /// <summary>
        /// Returns physical address list of device that matched the guid collection, if device is a interface, the function will get 
        /// the physical address of it's parent composite device
        /// </summary>
        /// <param name="strDeviceName"></param>
        /// <param name="strPhysicalAddressCollection"></param>
        public void GetUSBDevicePhysicalAddressByName(string strDeviceName, ref List<string> strPhysicalAddressCollection)
        {
            if (strPhysicalAddressCollection == null) { return; }
            strPhysicalAddressCollection.Clear();
            string DevEnum = REGSTR_KEY_USB;
            // to generate a list of all USB devices
            UpdateUSBDetectionLog("Searching Device Name:" + strDeviceName);
            IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
            if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
            {
                string KeyName;

                bool Success = true;
                uint deviceIndex = 0;
                while (Success)
                {
                    // create a Device Interface Data structure
                    SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                    deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                    // start the enumeration
                    Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                    if (Success)
                    {
                        KeyName = GetDeviceDescription(deviceInfoSet, deviceInfoData);
                        // is it a match?
                        if (KeyName != null && KeyName.IndexOf(strDeviceName) >= 0)
                        {
                            string thisPhysicalAddress = GetPhysicalAddress(deviceInfoSet, deviceInfoData);
                            if (thisPhysicalAddress != null)
                            {
                                if (strPhysicalAddressCollection.Contains(thisPhysicalAddress) == false)
                                {
                                    strPhysicalAddressCollection.Add(thisPhysicalAddress);
                                }
                                else
                                {
                                    //UpdateUSBDetectionLog("Physical Address of " + KeyName + " already exists:" + thisPhysicalAddress);
                                }
                            }
                            else
                            {
                                UpdateUSBDetectionLog("Fail to get Physical Address of devInst:" + deviceInfoData.DevInst.ToString());
                            }
                        }
                        else
                        {
                            UpdateUSBDetectionLog("Fail to get Device Description of devInst:" + deviceInfoData.DevInst.ToString());
                        }
                    }
                    deviceIndex++;
                }
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        /// <summary>
        /// Returns physical address list of device that matched the guid collection, if device is a interface, the function will get 
        /// the physical address of it's parent composite device
        /// </summary>
        /// <param name="guidCollection"></param>
        /// <param name="strPhysicalAddressCollection"></param>
        /// <param name="intPortNumberCollection"></param>
        public void GetUSBDevicePhysicalAddressByGuid(List<string> guidCollection, ref List<string> strPhysicalAddressCollection, ref List<List<int>> intPortNumberCollection)
        {
            if (strPhysicalAddressCollection == null) { return; }
            string DevEnum = REGSTR_KEY_USB;
            strPhysicalAddressCollection.Clear();
            foreach (string guid in guidCollection)
            {
                // to generate a list of all USB devices
                UpdateUSBDetectionLog("Searching GUID:" + guid);
                IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
                if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    bool Success = true;
                    uint deviceIndex = 0;
                    while (Success)
                    {
                        // create a Device Interface Data structure
                        SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                        deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                        // start the enumeration
                        Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                        if (Success)
                        {
                            string ClassGuid = GetClassGUID(deviceInfoSet, deviceInfoData);
                            if (ClassGuid != null && ClassGuid.ToLower() == guid.ToLower())
                            {
                                string thisPhysicalAddress = GetPhysicalAddress(deviceInfoSet, deviceInfoData);
                                if (thisPhysicalAddress != null)
                                {
                                    if (guid == GUID_PORT)//if guid is port, returns port nmber
                                    {
                                        UpdateUSBDetectionLog("Port Class Found on:" + thisPhysicalAddress);
                                        int thisPortNumber = GetComPortNumber(deviceInfoSet, deviceInfoData);
                                        if (thisPortNumber != 0)
                                        {
                                            UpdateUSBDetectionLog("Port Number:" + thisPortNumber.ToString());
                                            if (strPhysicalAddressCollection.Contains(thisPhysicalAddress) == false)
                                            {
                                                strPhysicalAddressCollection.Add(thisPhysicalAddress);
                                                if (intPortNumberCollection != null)
                                                {
                                                    intPortNumberCollection.Add(new List<int>());
                                                }
                                            }
                                            int index = strPhysicalAddressCollection.IndexOf(thisPhysicalAddress);
                                            if (index < 0)
                                            {
                                                UpdateUSBDetectionLog("Out of bounds!!!!");
                                            }
                                            else if (intPortNumberCollection != null)
                                            {
                                                //if one physical address contains more comport, add both of them
                                                List<int> portCollection = intPortNumberCollection[index];
                                                if (portCollection.Contains(thisPortNumber) == false)
                                                {
                                                    portCollection.Add(thisPortNumber);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            UpdateUSBDetectionLog("Fail to get port number on:" + thisPhysicalAddress + ",ignored");
                                        }
                                    }
                                    else if (strPhysicalAddressCollection.Contains(thisPhysicalAddress) == false)
                                    {
                                        strPhysicalAddressCollection.Add(thisPhysicalAddress);
                                        if (intPortNumberCollection != null)
                                        {
                                            intPortNumberCollection.Add(new List<int>());
                                        }
                                    }
                                    else
                                    {
                                        //UpdateUSBDetectionLog("Physical Address:" + thisPhysicalAddress + " already exists:");
                                    }
                                }
                                else
                                {
                                    UpdateUSBDetectionLog("Fail to get Physical Address of devInst:" + deviceInfoData.DevInst.ToString());
                                }
                            }
                        }
                        deviceIndex++;
                    }
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
        }

        /// <summary>
        /// Returns physical address list of device that matched the guid collection, if device is a interface, the function will get 
        /// the physical address of it's parent composite device
        /// </summary>
        /// <param name="strPhysicalAddress"></param>
        /// <param name="intPortNumberCollection"></param>
        public void GetPortNumberByUSBDevicePhysicalAddress(string strPhysicalAddress, ref List<int> intPortNumberCollection)
        {
            if (strPhysicalAddress == null) { return; }
            string DevEnum = REGSTR_KEY_USB;
            //string guid = GUID_PORT;
            string guid = GUID_HUB;

            // to generate a list of all USB devices
            UpdateUSBDetectionLog("Searching physical address:" + strPhysicalAddress);
            IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
            if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
            {
                bool Success = true;
                uint deviceIndex = 0;
                while (Success)
                {
                    // create a Device Interface Data structure
                    SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                    deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                    // start the enumeration
                    Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                    if (Success)
                    {
                        string ClassGuid = GetClassGUID(deviceInfoSet, deviceInfoData);
                        if (ClassGuid != null && ClassGuid.ToLower() == guid.ToLower())
                        {
                            string thisPhysicalAddress = GetPhysicalAddress(deviceInfoSet, deviceInfoData);
                            if (thisPhysicalAddress != null && thisPhysicalAddress.IndexOf(strPhysicalAddress) == 0) // the parent\current device of this port
                            {
                                int thisPortNumber = GetComPortNumber(deviceInfoSet, deviceInfoData);
                                if (thisPortNumber > 0)
                                {
                                    UpdateUSBDetectionLog("Port Number:" + thisPortNumber.ToString());
                                    intPortNumberCollection.Add(thisPortNumber);
                                }
                                else
                                {
                                    UpdateUSBDetectionLog("Fail to get port number on:" + thisPhysicalAddress + ",ignored");
                                }
                            }
                            else
                            {
                                UpdateUSBDetectionLog("Fail to get Physical Address of devInst:" + deviceInfoData.DevInst.ToString());
                            }
                        }
                    }
                    deviceIndex++;
                }
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        public class USBPortDetails
        {
            public string PortDescription;
            public string PortPhysicalAddress;
            public USBPortDetails(string portDescription, string portPhysicalAddress)
            {
                PortDescription = portDescription;
                PortPhysicalAddress = portPhysicalAddress;
            }
        }

        public class USB_PORT_INFO_LIST
        {
            Dictionary<int, USBPortDetails> USBPortTable = new Dictionary<int, USBPortDetails>();

            public USBPortDetails this[int portno]
            {
                get
                {
                    return USBPortTable[portno];
                }
            }

            public bool ContainsPortNumber(int portNumber)
            {
                return USBPortTable.ContainsKey(portNumber);
            }

            public int FirstCOMNumberByPortDetails(USBPortDetails portDetails)
            {
                if (portDetails == null) { return -1; }
                foreach (int portno in USBPortTable.Keys)
                {
                    bool found = true;
                    if (portDetails.PortDescription != null)
                    {
                        if (USBPortTable[portno].PortDescription != portDetails.PortDescription)
                        {
                            found = false;
                        }
                    }
                    if (portDetails.PortPhysicalAddress != null)
                    {
                        if (USBPortTable[portno].PortPhysicalAddress != portDetails.PortPhysicalAddress)
                        {
                            found = false;
                        }
                    }
                    if (found) { return portno; }
                }
                return -1;
            }

            public Dictionary<int, USBPortDetails>.KeyCollection PortNumbers
            {
                get { return USBPortTable.Keys; }
            }

            public Dictionary<int, USBPortDetails>.ValueCollection PortDetails
            {
                get { return USBPortTable.Values; }
            }

            public void Add(int portNumber, string portDescription, string portPhysicalAddress)
            {
                USBPortTable.Add(portNumber, new USBPortDetails(portDescription, portPhysicalAddress));
            }

            public void Clear()
            {
                USBPortTable.Clear();
            }

            public int Count
            {
                get
                {
                    try
                    {
                        return this.USBPortTable.Count;
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }

            public IEnumerator GetEnumerator()
            {
                try
                {
                    return this.USBPortTable.GetEnumerator();
                }
                catch
                {
                    return null;
                }
            }

            public bool IsSameAs(USB_PORT_INFO_LIST obj)
            {
                if (USBPortTable.Count != obj.Count) { return false; }
                foreach (int portno in USBPortTable.Keys)
                {
                    if (obj.FirstCOMNumberByPortDetails(USBPortTable[portno]) != portno) { return false; }
                }
                return true;
            }
        }

        public void GetPortNumberListWithInformation(ref USB_PORT_INFO_LIST lstUSBPortInformationCollection)
        {
            if (lstUSBPortInformationCollection == null)
            {
                return;
            }
            lstUSBPortInformationCollection.Clear();
            string DevEnum = REGSTR_KEY_USB;

            // to generate a list of all USB devices
            IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
            if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
            {
                bool Success = true;
                uint deviceIndex = 0;
                while (Success)
                {
                    // create a Device Interface Data structure
                    SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                    deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);
                    string guid = GUID_PORT;
                    // start the enumeration
                    Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                    if (Success)
                    {
                        string ClassGuid = GetClassGUID(deviceInfoSet, deviceInfoData);
                        if (ClassGuid != null && ClassGuid.ToLower() == guid.ToLower())
                        {
                            string thisPortDescription = "";
                            int thisPortNumber = GetPortNumberWithDescription(deviceInfoSet, deviceInfoData, ref thisPortDescription);
                            if (thisPortNumber > 0)
                            {
                                string thisPhysicalAddress = GetPhysicalAddress(deviceInfoSet, deviceInfoData);
                                if (thisPhysicalAddress != null)
                                {
                                    if (lstUSBPortInformationCollection.ContainsPortNumber(thisPortNumber) == false)
                                    {
                                        lstUSBPortInformationCollection.Add(thisPortNumber, thisPortDescription, thisPhysicalAddress);
                                    }
                                    else
                                    {
                                        UpdateUSBDetectionLog("Multiply port contains same port number:COM" + thisPortNumber.ToString());
                                    }
                                }
                                else
                                {
                                    UpdateUSBDetectionLog("Fail to get Physical Address of devInst:" + deviceInfoData.DevInst.ToString());
                                }
                            }
                            else
                            {
                                UpdateUSBDetectionLog("Fail to get Port Number of devInst:" + deviceInfoData.DevInst.ToString());
                            }
                        }
                    }
                    deviceIndex++;
                }
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        /// <summary>
        /// Returns existing comport list in current PC
        /// </summary>
        /// <param name="strPortNumberCollection"></param>
        public void GetPortNumberList(ref List<int> intPortNumberCollection)
        {
            if (intPortNumberCollection == null) { return; }
            USB_PORT_INFO_LIST PortInfoList = new USB_PORT_INFO_LIST();
            GetPortNumberListWithInformation(ref PortInfoList);
            foreach (int portno in PortInfoList.PortNumbers)
            {
                intPortNumberCollection.Add(portno);
            }
        }

        #endregion

        #region Util

        internal string GetPhysicalAddress(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData)
        {
            IntPtr deviceInfoSet = DeviceInfoSet;
            SP_DEVINFO_DATA deviceInfoData = DeviceInfoData;
            IntPtr ptrBuf = Marshal.AllocHGlobal(BUFFER_SIZE);
            uint RequiredSize = 0;
            uint RegType = REG_SZ;

            string thisPhysicalAddress = null;

            // Get the location path
            if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_LOCATION_PATHS, ref RegType, ptrBuf, BUFFER_SIZE, ref RequiredSize))
            {
                thisPhysicalAddress = Marshal.PtrToStringAuto(ptrBuf);

                //Get the location information
                if (SetupDiGetDeviceRegistryProperty(DeviceInfoSet, ref DeviceInfoData, SPDRP_LOCATION_INFORMATION, ref RegType, ptrBuf, BUFFER_SIZE, ref RequiredSize))
                {
                    string strLocationInformation = Marshal.PtrToStringAuto(ptrBuf);
                    // The location information should be Port_#XXXX.Hub_#XXXX for a usb device and
                    // xxxx.xxxx.xxxx.xxxx.xxxx for a interface, so we check the string format here to determine
                    // if it's a interface of a device
                    try
                    {
                        Match match = Regex.Match(strLocationInformation, "Port_#[0-9|A-F]{4}.Hub_#[0-9|A-F]{4}");
                        if (match.Success == false)
                        {
                            //It's a interface get the parent device' device location path
                            UpdateUSBDetectionLog("A interface device, format physical address of partent");
                            //thisPhysicalAddress = GetParentDevicePhysicalAddress(thisPhysicalAddress);
                            thisPhysicalAddress = null;
                        }
                    }
                    catch
                    {
                        UpdateUSBDetectionLog("type unknown device, format physical address of partent");
                        //thisPhysicalAddress = GetParentDevicePhysicalAddress(thisPhysicalAddress);
                        thisPhysicalAddress = null;
                    }
                }
                else
                {
                    UpdateUSBDetectionLog("Fail to get Location Information of devInst:" + deviceInfoData.DevInst.ToString());
                    thisPhysicalAddress = null;
                }
            }
            else
            {
                UpdateUSBDetectionLog("Fail to get Location Path of devInst:" + deviceInfoData.DevInst.ToString());
                thisPhysicalAddress = null;
            }

            Marshal.FreeHGlobal(ptrBuf);

            return thisPhysicalAddress;
        }

        internal string GetPhysicalAddress_Diagnostics(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData)
        {
            IntPtr deviceInfoSet = DeviceInfoSet;
            SP_DEVINFO_DATA deviceInfoData = DeviceInfoData;
            IntPtr ptrBuf = Marshal.AllocHGlobal(BUFFER_SIZE);
            uint RequiredSize = 0;
            uint RegType = REG_SZ;

            string thisPhysicalAddress = null;

            // Get the location path
            if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_LOCATION_PATHS, ref RegType, ptrBuf, BUFFER_SIZE, ref RequiredSize))
            {
                thisPhysicalAddress = Marshal.PtrToStringAuto(ptrBuf);

                ////Get the location information
                //if (SetupDiGetDeviceRegistryProperty(DeviceInfoSet, ref DeviceInfoData, SPDRP_LOCATION_INFORMATION, ref RegType, ptrBuf, BUFFER_SIZE, ref RequiredSize))
                //{
                //    string strLocationInformation = Marshal.PtrToStringAuto(ptrBuf);
                //    // The location information should be Port_#XXXX.Hub_#XXXX for a usb device and
                //    // xxxx.xxxx.xxxx.xxxx.xxxx for a interface, so we check the string format here to determine
                //    // if it's a interface of a device
                //    try
                //    {
                //        Match match = Regex.Match(strLocationInformation, "Port_#[0-9|A-F]{4}.Hub_#[0-9|A-F]{4}");
                //        if (match.Success == false)
                //        {
                //            //It's a interface get the parent device' device location path
                //            UpdateUSBDetectionLog("A interface device, format physical address of partent");
                //            //thisPhysicalAddress = GetParentDevicePhysicalAddress(thisPhysicalAddress);
                //            thisPhysicalAddress = null;
                //        }
                //    }
                //    catch
                //    {
                //        UpdateUSBDetectionLog("type unknown device, format physical address of partent");
                //        //thisPhysicalAddress = GetParentDevicePhysicalAddress(thisPhysicalAddress);
                //        thisPhysicalAddress = null;
                //    }
                //}
                //else
                //{
                //    UpdateUSBDetectionLog("Fail to get Location Information of devInst:" + deviceInfoData.DevInst.ToString());
                //    thisPhysicalAddress = null;
                //}
            }
            else
            {
                UpdateUSBDetectionLog("Fail to get Location Path of devInst:" + deviceInfoData.DevInst.ToString());
                thisPhysicalAddress = null;
            }

            Marshal.FreeHGlobal(ptrBuf);

            return thisPhysicalAddress;
        }

        internal string GetPhysicalAddress_BAK(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData)
        {
            //Get windows version
            IntPtr deviceInfoSet = DeviceInfoSet;
            SP_DEVINFO_DATA deviceInfoData = DeviceInfoData;
            IntPtr ptrBuf = Marshal.AllocHGlobal(BUFFER_SIZE);
            uint RequiredSize = 0;
            uint RegType = REG_SZ;
            string thisPhysicalAddress = null;
            OSVersionInfo osvi = new OSVersionInfo();
            osvi.OSVersionInfoSize = Marshal.SizeOf(osvi);
            GetVersionEx(osvi);
            double WinVer = osvi.MajorVersion + osvi.MinorVersion * 0.1;
            //if (WinVer >= 5.2) location paths is working on some PC
            if (WinVer < 0) //Apply for all windows OS
            {
                //For Windows Server 2003 or later with location path
                //Get the location path
                if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_LOCATION_PATHS, ref RegType, ptrBuf, BUFFER_SIZE, ref RequiredSize))
                {
                    thisPhysicalAddress = Marshal.PtrToStringAuto(ptrBuf);
                    //Get the location information
                    if (SetupDiGetDeviceRegistryProperty(DeviceInfoSet, ref DeviceInfoData, SPDRP_LOCATION_INFORMATION, ref RegType, ptrBuf, BUFFER_SIZE, ref RequiredSize))
                    {
                        string strLocationInformation = Marshal.PtrToStringAuto(ptrBuf);
                        // The location information should be Port_#XXXX.Hub_#XXXX for a usb device and
                        // xxxx.xxxx.xxxx.xxxx.xxxx for a interface, so we check the string format here to determine
                        // if it's a interface of a device
                        try
                        {
                            Match match = Regex.Match(strLocationInformation, "Port_#[0-9|A-F]{4}.Hub_#[0-9|A-F]{4}");
                            if (match.Success == false)
                            {
                                //It's a interface get the parent device' device location path
                                UpdateUSBDetectionLog("A interface device, format physical address of partent");
                                thisPhysicalAddress = GetParentDevicePhysicalAddress(thisPhysicalAddress);
                            }
                        }
                        catch
                        {
                            UpdateUSBDetectionLog("type unknown device, format physical address of partent");
                            thisPhysicalAddress = GetParentDevicePhysicalAddress(thisPhysicalAddress);
                        }
                    }
                    else
                    {
                        UpdateUSBDetectionLog("Fail to get Location Information of devInst:" + deviceInfoData.DevInst.ToString());
                    }
                }
                else
                {
                    UpdateUSBDetectionLog("Fail to get Location Path of devInst:" + deviceInfoData.DevInst.ToString());
                }
            }
            else
            {
                //This part for win XP or earlier, these doesn't support location path information
                if (CM_Get_Device_ID(deviceInfoData.DevInst, ptrBuf, BUFFER_SIZE, 0) == CR_SUCCESS)
                {
                    string DeviceID = Marshal.PtrToStringAuto(ptrBuf);
                    Int32 ptrParent = INVALID_HANDLE_VALUE;
                    Int32 cr = CR_SUCCESS;
                    Int32 ptrDevice = INVALID_HANDLE_VALUE;
                    try
                    {
                        Match match = Regex.Match(DeviceID, "VID_[0-9|A-F]{4}&PID_[0-9|A-F]{4}&MI_[0-9|A-F]{2}");
                        // If device contains MI_XX, it's a interface
                        if (match.Success)
                        {
                            //It's a interface, get the parent device
                            UpdateUSBDetectionLog("A interface device, try get parent node device");
                            cr = CM_Get_Parent(ref ptrDevice, deviceInfoData.DevInst, 0);
                        }
                        else
                        {
                            //Not a interface
                            ptrDevice = deviceInfoData.DevInst;
                        }
                    }
                    catch
                    {
                        ptrDevice = deviceInfoData.DevInst;
                    }
                    if (cr == CR_SUCCESS && ptrDevice != INVALID_HANDLE_VALUE)
                    {
                        UpdateUSBDetectionLog("Parent node device:" + ptrDevice.ToString());
                        //Get parent device's device instance which is a hub port
                        if (CM_Get_Parent(ref ptrParent, ptrDevice, 0) == CR_SUCCESS &&
                            ptrParent != INVALID_HANDLE_VALUE)
                        {
                            if (CM_Get_Device_ID(ptrParent, ptrBuf, BUFFER_SIZE, 0) == CR_SUCCESS)
                            {

                                string parentPhysicalAddress = Marshal.PtrToStringAuto(ptrBuf);
                                //Get Hub Port Index
                                string guid = GUID_HUB_DRIVER; //HUB driver GUID
                                string ParentPNPDevicePath = "\\\\.\\" + parentPhysicalAddress.Replace('\\', '#') + "#" + guid;
                                //Get the driver key of present device.
                                string DriverKeyName = GetCMPropertyString(ptrDevice, CM_DRP_DRIVER);
                                if (DriverKeyName != null)
                                {
                                    //Go through the driver key of every hub port and get the connection index of our devices
                                    int ConnectIndex = GetConnectionIndex(ParentPNPDevicePath, DriverKeyName);
                                    if (ConnectIndex > 0)
                                    {
                                        thisPhysicalAddress = parentPhysicalAddress + "\\PORT" + Convert.ToString(ConnectIndex);
                                    }
                                    else
                                    {
                                        UpdateUSBDetectionLog("Fail to get parent(" + ptrParent.ToString() + ") connection index of devInst:" + deviceInfoData.DevInst.ToString());
                                    }
                                }
                                else
                                {
                                    UpdateUSBDetectionLog("Fail to get parent(" + ptrParent.ToString() + ") driver key name of devInst:" + deviceInfoData.DevInst.ToString());
                                }
                            }
                            else
                            {
                                UpdateUSBDetectionLog("Fail to get parent(" + ptrParent.ToString() + ") device ID of devInst:" + deviceInfoData.DevInst.ToString());
                            }
                        }
                        else
                        {
                            UpdateUSBDetectionLog("CM Fail to get parent device");
                        }
                    }
                    else
                    {
                        UpdateUSBDetectionLog("CM Fail to get parent node device:" + ptrDevice.ToString());
                    }
                }
                else
                {
                    UpdateUSBDetectionLog("Fail to get device ID of devInst:" + deviceInfoData.DevInst.ToString());
                }
            }
            Marshal.FreeHGlobal(ptrBuf);
            return thisPhysicalAddress;
        }

        internal string GetCMPropertyString(int ptrDevice, int cmProperty)
        {
            string strCMProperty = null;
            UInt32 RequiredSize = 0;
            Microsoft.Win32.RegistryValueKind kind;
            IntPtr ptrBuf = Marshal.AllocHGlobal(BUFFER_SIZE);
            CM_Get_DevNode_Registry_Property(ptrDevice, cmProperty, out kind, IntPtr.Zero, ref RequiredSize, 0);
            if (RequiredSize > 0 && kind == Microsoft.Win32.RegistryValueKind.String)
            {
                if (CM_Get_DevNode_Registry_Property(ptrDevice, cmProperty, out kind, ptrBuf, ref RequiredSize, 0) == CR_SUCCESS)
                {
                    strCMProperty = Marshal.PtrToStringAnsi(ptrBuf);
                }
            }
            Marshal.FreeHGlobal(ptrBuf);
            return strCMProperty;
        }

        internal string GetClassGUID(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData)
        {
            IntPtr deviceInfoSet = DeviceInfoSet;
            SP_DEVINFO_DATA deviceInfoData = DeviceInfoData;
            IntPtr ptrBuf = Marshal.AllocHGlobal(BUFFER_SIZE);
            uint RequiredSize = 0;
            uint RegType = REG_SZ;
            string ClassGuid = null;
            if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_CLASSGUID, ref RegType, ptrBuf, BUFFER_SIZE, ref RequiredSize))
            {
                ClassGuid = Marshal.PtrToStringAuto(ptrBuf);
            }
            Marshal.FreeHGlobal(ptrBuf);
            return ClassGuid;
        }

        internal string GetDeviceID(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData)
        {
            IntPtr deviceInfoSet = DeviceInfoSet;
            SP_DEVINFO_DATA deviceInfoData = DeviceInfoData;
            IntPtr ptrBuf = Marshal.AllocHGlobal(BUFFER_SIZE);

            string DeviceID = "";
            if (CM_Get_Device_ID(deviceInfoData.DevInst, ptrBuf, BUFFER_SIZE, 0) == CR_SUCCESS)
            {
                DeviceID = Marshal.PtrToStringAuto(ptrBuf);
            }

            return DeviceID;
        }

        internal string GetDeviceDescription(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData)
        {
            IntPtr deviceInfoSet = DeviceInfoSet;
            SP_DEVINFO_DATA deviceInfoData = DeviceInfoData;
            IntPtr ptrBuf = Marshal.AllocHGlobal(BUFFER_SIZE);
            uint RequiredSize = 0;
            uint RegType = REG_SZ;
            string KeyName = null;
            try
            {
                if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_FRIENDLYNAME, ref RegType, ptrBuf, BUFFER_SIZE, ref RequiredSize))
                {
                    KeyName = Marshal.PtrToStringAuto(ptrBuf);
                }
                else if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_DEVICEDESC, ref RegType, ptrBuf, BUFFER_SIZE, ref RequiredSize))
                {
                    KeyName = Marshal.PtrToStringAuto(ptrBuf);
                }

                if (KeyName != null)
                {
                    bool bFound = false;
                    if (PORT_KEY.Count == 0)
                    {
                        bFound = true;
                    }
                    else
                    {
                        foreach (string portKey in PORT_KEY)
                        {
                            Match match;
                            if (PORT_KEY_CASE_SENS)
                            {
                                match = Regex.Match(KeyName, portKey);
                            }
                            else
                            {
                                match = Regex.Match(KeyName.ToLower(), portKey.ToLower());
                            }
                            if (match.Success)
                            {
                                bFound = true;
                            }
                        }
                    }
                    if (bFound == false)
                    {
                        KeyName = null;
                    }
                }
            }
            catch (Exception)
            {
                //Fail
                KeyName = null;
            }
            finally
            {
                Marshal.FreeHGlobal(ptrBuf);
            }
            return KeyName;
        }

        internal int GetPortNumberWithDescription(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData, ref string PortDescription)
        {
            IntPtr deviceInfoSet = DeviceInfoSet;
            SP_DEVINFO_DATA deviceInfoData = DeviceInfoData;
            IntPtr ptrBuf = Marshal.AllocHGlobal(BUFFER_SIZE);
            string thisPortNumber = "0";
            try
            {
                string thisDescription = GetDeviceDescription(deviceInfoSet, deviceInfoData);
                if (thisDescription != null)
                {
                    Match match = Regex.Match(thisDescription, "(COM[0-9])");
                    if (match.Success)
                    {
                        string str = "(COM";
                        int startIdx = thisDescription.IndexOf(str) + str.Length;
                        int endIdx = thisDescription.LastIndexOf(")");
                        thisPortNumber = thisDescription.Substring(startIdx, endIdx - startIdx);
                    }
                    if (PortDescription != null)
                        PortDescription = thisDescription;
                }
            }
            catch (Exception)
            {
                //Fail
            }
            finally
            {
                Marshal.FreeHGlobal(ptrBuf);
            }
            return Convert.ToInt32(thisPortNumber);
        }

        internal int GetComPortNumber(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData)
        {
            string portDescription = null;
            return GetPortNumberWithDescription(DeviceInfoSet, DeviceInfoData, ref portDescription);
        }

        internal string GetParentDevicePhysicalAddress(string devicePhysicalAddress)
        {
            return devicePhysicalAddress.Substring(0, devicePhysicalAddress.LastIndexOf('#'));
        }

        internal int GetConnectionIndex(String DevicePath, String DriverKeyName)
        {
            int ConnectionIndex = 0;
            IntPtr deviceInfoSet = CreateFile(DevicePath, GENERIC_WRITE, FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
            {
                int HubPortCount = 0;
                Int32 nBytesReturned;
                USB_NODE_INFORMATION Buffer = new USB_NODE_INFORMATION();
                if (DeviceIoControl(deviceInfoSet,
                    IOCTL_USB_GET_NODE_INFORMATION,
                    ref Buffer,
                    Marshal.SizeOf(Buffer),
                    ref Buffer,
                    Marshal.SizeOf(Buffer),
                    out nBytesReturned,
                    IntPtr.Zero))
                {
                    if (Buffer.NodeType == USB_HUB_NODE.UsbHub)
                    {
                        HubPortCount = Buffer.u.HubInformation.HubDescriptor.bNumberOfPorts;         // Port Number
                    }
                    else
                    {
                        HubPortCount = 0;
                        UpdateUSBDetectionLog("Device is not a Hub, devInst:" + deviceInfoSet.ToString());
                    }

                    // loop thru all of the ports on the hub
                    // BTW: Ports are numbered starting at 1
                    for (int i = 1; i <= HubPortCount; i++)
                    {
                        USB_NODE_CONNECTION_DRIVERKEY_NAME NodeKeyName = new USB_NODE_CONNECTION_DRIVERKEY_NAME();
                        NodeKeyName.ConnectionIndex = i;
                        if (DeviceIoControl(deviceInfoSet,
                            IOCTL_USB_GET_NODE_CONNECTION_DRIVERKEY_NAME,
                            ref NodeKeyName,
                            Marshal.SizeOf(NodeKeyName),
                            ref NodeKeyName,
                            Marshal.SizeOf(NodeKeyName),
                            out nBytesReturned,
                            IntPtr.Zero))
                        {
                            if (NodeKeyName.DriverKeyName == DriverKeyName)
                            {
                                ConnectionIndex = NodeKeyName.ConnectionIndex;
                            }
                        }
                        else
                        {
                            UpdateUSBDetectionLog("Fail to get connection drivery key name of devInst:" + deviceInfoSet.ToString() + "connection index:" + i.ToString());
                        }
                    }
                }
                else
                {
                    UpdateUSBDetectionLog("Fail to get Node Type of devInst:" + deviceInfoSet.ToString());
                }
                CloseHandle(deviceInfoSet);
            }
            return ConnectionIndex;
        }

        #endregion

        #region Function

        public bool GetUSBDevicePhysicalAddressByDeviceName(string strDeviceName, ref List<string> strPhysicalAddressCollection, ref string strErrorMessage)
        {
            try
            {
                if (strDeviceName == "")
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }
                if (strPhysicalAddressCollection == null)
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }
                strPhysicalAddressCollection.Clear();

                string DevEnum = REGSTR_KEY_USB;
                IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
                if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    string KeyName = "";
                    bool Success = true;
                    uint deviceIndex = 0;
                    while (Success)
                    {
                        // create a Device Interface Data structure
                        SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                        deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                        // start the enumeration
                        Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                        if (Success)
                        {
                            KeyName = GetDeviceDescription(deviceInfoSet, deviceInfoData);
                            if (KeyName != null && KeyName.IndexOf(strDeviceName) >= 0)
                            {
                                string thisPhysicalAddress = GetPhysicalAddress(deviceInfoSet, deviceInfoData);

                                if (thisPhysicalAddress != null)
                                {
                                    if (strPhysicalAddressCollection.Contains(thisPhysicalAddress) == false)
                                    {
                                        strPhysicalAddressCollection.Add(thisPhysicalAddress);
                                    }
                                }
                                else
                                {
                                    //UpdateUSBDetectionLog("Fail to get Physical Address of devInst:" + deviceInfoData.DevInst.ToString());
                                }
                            }
                            else
                            {
                                //UpdateUSBDetectionLog("Fail to get Device Description of devInst:" + deviceInfoData.DevInst.ToString());
                            }
                        }
                        deviceIndex++;
                    }

                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
                else
                {
                    strErrorMessage = "Fail to enum deviceInfoSet.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            return true;
        }

        public bool GetSerianNumberByUSBDevicePhysicalAddress(string strPhysicalAddress, string strDeviceName, ref List<string> strSNCollection, ref string strErrorMessage)
        {
            try
            {
                if (strPhysicalAddress == "")
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }
                if (strDeviceName == "")
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }
                if (strSNCollection == null)
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }

                // Generate a list of all USB devices
                string DevEnum = REGSTR_KEY_USB;
                string guid = GUID_HUB;
                IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
                if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    string KeyName = "";
                    bool Success = true;
                    uint deviceIndex = 0;
                    while (Success)
                    {
                        // create a Device Interface Data structure
                        SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                        deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                        // start the enumeration
                        Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                        if (Success)
                        {
                            KeyName = GetDeviceDescription(deviceInfoSet, deviceInfoData);
                            string ClassGuid = GetClassGUID(deviceInfoSet, deviceInfoData);
                            if (ClassGuid != null && ClassGuid.ToLower() == guid.ToLower())
                            {
                                string thisPhysicalAddress = GetPhysicalAddress(deviceInfoSet, deviceInfoData);
                                if (thisPhysicalAddress != null && thisPhysicalAddress.IndexOf(strPhysicalAddress) == 0) // the parent\current device of this port
                                {
                                    if (KeyName != null && KeyName.IndexOf(strDeviceName) >= 0)
                                    {
                                        string strDeviceID = GetDeviceID(deviceInfoSet, deviceInfoData);
                                        if (strDeviceID.Length > 0)
                                        {
                                            string[] arrTemp = strDeviceID.Split('\\');

                                            string strSN = arrTemp[arrTemp.Length - 1].ToString();
                                            if (strSNCollection.Contains(strSN) == false)
                                            {
                                                strSNCollection.Add(strSN);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //UpdateUSBDetectionLog("Fail to get port number on:" + thisPhysicalAddress + ",ignored");
                                    }
                                }
                                else
                                {
                                    //UpdateUSBDetectionLog("Fail to get Physical Address of devInst:" + deviceInfoData.DevInst.ToString());
                                }
                            }
                        }
                        deviceIndex++;
                    }
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            return true;
        }

        public bool GetSerianNumberByDeviceName(string strDeviceName, ref List<string> strSNCollection, ref string strErrorMessage)
        {
            try
            {
                if (strDeviceName == "")
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }
                if (strSNCollection == null)
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }

                string DevEnum = REGSTR_KEY_USB;
                IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
                if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    string KeyName = "";
                    bool Success = true;
                    uint deviceIndex = 0;
                    while (Success)
                    {
                        // create a Device Interface Data structure
                        SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                        deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                        // start the enumeration
                        Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                        if (Success)
                        {
                            KeyName = GetDeviceDescription(deviceInfoSet, deviceInfoData);
                            if (KeyName != null && KeyName.IndexOf(strDeviceName) >= 0)
                            {
                                string strDeviceID = GetDeviceID(deviceInfoSet, deviceInfoData);
                                if (strDeviceID.Length > 0)
                                {
                                    string[] arrTemp = strDeviceID.Split('\\');

                                    string strSN = arrTemp[arrTemp.Length - 1].ToString();
                                    if (strSNCollection.Contains(strSN) == false)
                                    {
                                        strSNCollection.Add(strSN);
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                        deviceIndex++;
                    }
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            return true;
        }

        public bool GetSerianNumberByDeviceName(string strDeviceName, ref string strSerialNumber, ref string strErrorMessage)
        {
            if (strDeviceName == "")
            {
                strErrorMessage = "Invalid input para.";
                return false;
            }

            strSerialNumber = "";
            List<string> strSNCollection = new List<string>();

            try
            {
                string DevEnum = REGSTR_KEY_USB;
                IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
                if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    string KeyName = "";
                    bool Success = true;
                    uint deviceIndex = 0;
                    while (Success)
                    {
                        // create a Device Interface Data structure
                        SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                        deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                        // start the enumeration
                        Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                        if (Success)
                        {
                            KeyName = GetDeviceDescription(deviceInfoSet, deviceInfoData);
                            if (KeyName != null && KeyName.IndexOf(strDeviceName) >= 0)
                            {
                                string strDeviceID = GetDeviceID(deviceInfoSet, deviceInfoData);
                                if (strDeviceID.Length > 0)
                                {
                                    string[] arrTemp = strDeviceID.Split('\\');

                                    string strSN = arrTemp[arrTemp.Length - 1].ToString();
                                    if (strSNCollection.Contains(strSN) == false)
                                    {
                                        strSNCollection.Add(strSN);
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                        deviceIndex++;
                    }
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            if (strSNCollection.Count == 0)
            {
                strErrorMessage = "SN count is 0";
                return false;
            }
            if (strSNCollection.Count > 1)
            {
                strErrorMessage = "SN count is " + strSNCollection.Count.ToString();
                return false;
            }

            strSerialNumber = strSNCollection[0].ToString();

            return true;
        }

        public bool CheckADBConnectByUSBDevicePhysicalAddress(string strPhysicalAddress, ref string strErrorMessage)
        {
            bool bRes = false;

            try
            {
                if (strPhysicalAddress == "")
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }

                // Generate a list of all USB devices
                string DevEnum = REGSTR_KEY_USB;
                string guid = GUID_ADB;
                IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
                if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    bool Success = true;
                    uint deviceIndex = 0;
                    while (Success)
                    {
                        // create a Device Interface Data structure
                        SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                        deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                        // start the enumeration
                        Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                        if (Success)
                        {
                            String KeyName = GetDeviceDescription(deviceInfoSet, deviceInfoData);
                            string ClassGuid = GetClassGUID(deviceInfoSet, deviceInfoData);
                            if (ClassGuid != null && ClassGuid.ToLower() == guid.ToLower())
                            {
                                string thisPhysicalAddress = GetPhysicalAddress_Diagnostics(deviceInfoSet, deviceInfoData);
                                if (thisPhysicalAddress != null && thisPhysicalAddress.IndexOf(strPhysicalAddress) == 0) // the parent\current device of this port
                                {
                                    bRes = true;
                                }
                                else
                                {
                                    bRes = false;
                                }
                            }
                        }
                        deviceIndex++;
                    }
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            if (bRes == false)
            {
                return false;
            }

            return true;
        }

        public bool CheckPortConnectByUSBDevicePhysicalAddress(string strPhysicalAddress, string strPortName, ref string strErrorMessage)
        {
            bool bRes = false;

            try
            {
                if (strPhysicalAddress == "")
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }
                if (strPortName == "")
                {
                    strErrorMessage = "Invalid input para.";
                    return false;
                }

                // Generate a list of all USB devices
                string DevEnum = REGSTR_KEY_USB;
                string guid = GUID_PORT;
                IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
                if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    bool Success = true;
                    uint deviceIndex = 0;
                    while (Success)
                    {
                        // create a Device Interface Data structure
                        SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                        deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                        // start the enumeration
                        Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                        if (Success)
                        {
                            String KeyName = GetDeviceDescription(deviceInfoSet, deviceInfoData);
                            string ClassGuid = GetClassGUID(deviceInfoSet, deviceInfoData);
                            if (ClassGuid != null && ClassGuid.ToLower() == guid.ToLower())
                            {
                                string thisPhysicalAddress = GetPhysicalAddress_Diagnostics(deviceInfoSet, deviceInfoData);
                                if (thisPhysicalAddress != null && thisPhysicalAddress.IndexOf(strPhysicalAddress) == 0) // the parent\current device of this port
                                {
                                    if (KeyName.Contains(strPortName) == true)
                                    {
                                        bRes = true;
                                    }
                                }
                                else
                                {

                                }
                            }
                        }
                        deviceIndex++;
                    }
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            if (bRes == false)
            {
                return false;
            }

            return true;
        }

        public bool GetPortByUSBDevicePhysicalAddress(string strPhysicalAddress, ref List<string> strPortNumberCollection, ref string strErrorMessage)
        {
            try
            {
                if (strPhysicalAddress == "")
                {
                    return false;
                }
                if (strPortNumberCollection == null)
                {
                    return false;
                }

                string DevEnum = REGSTR_KEY_USB;
                string guid = GUID_PORT;
                //string guid = GUID_HUB;

                // to generate a list of all USB devices
                //UpdateUSBDetectionLog("Searching physical address:" + strPhysicalAddress);
                IntPtr deviceInfoSet = SetupDiGetClassDevs(0, DevEnum, IntPtr.Zero, (DIGCF_PRESENT | DIGCF_ALLCLASSES));
                if (deviceInfoSet.ToInt32() != INVALID_HANDLE_VALUE)
                {
                    bool Success = true;
                    uint deviceIndex = 0;
                    while (Success)
                    {
                        // create a Device Interface Data structure
                        SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                        deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                        // start the enumeration
                        Success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData);
                        if (Success)
                        {
                            string ClassGuid = GetClassGUID(deviceInfoSet, deviceInfoData);
                            if (ClassGuid != null && ClassGuid.ToLower() == guid.ToLower())
                            {
                                string thisPhysicalAddress = GetPhysicalAddress_Diagnostics(deviceInfoSet, deviceInfoData);
                                if (thisPhysicalAddress != null && thisPhysicalAddress.IndexOf(strPhysicalAddress) == 0) // the parent\current device of this port
                                {
                                    int thisPortNumber = GetComPortNumber(deviceInfoSet, deviceInfoData);
                                    if (thisPortNumber > 0)
                                    {
                                        //UpdateUSBDetectionLog("Port Number:" + thisPortNumber.ToString());
                                        strPortNumberCollection.Add(thisPortNumber.ToString());
                                    }
                                    else
                                    {
                                        //UpdateUSBDetectionLog("Fail to get port number on:" + thisPhysicalAddress + ",ignored");
                                    }
                                }
                                else
                                {
                                    //UpdateUSBDetectionLog("Fail to get Physical Address of devInst:" + deviceInfoData.DevInst.ToString());
                                }
                            }
                        }
                        deviceIndex++;
                    }
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            return true;
        }

        #endregion
    }

}
