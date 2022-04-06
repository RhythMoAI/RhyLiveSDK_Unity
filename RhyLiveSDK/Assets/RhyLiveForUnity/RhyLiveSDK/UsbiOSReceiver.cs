using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using iMobileDevice;
using iMobileDevice.Usbmuxd;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;

namespace RhythMo.RhyLiveSDK
{

public class UsbiOSReceiver : MonoBehaviour
{
    private static readonly int RetryConnectionFrameInterval = 400;
    public bool Connected {get; private set;} = false;
    public ushort RemotePort {get; set;} = 0;
    public delegate void RawUpdaterFunc(byte[] data, uint valid_len);
    public RawUpdaterFunc RawUpdater {get; set;} = null;

    private Task runningiOSReceier = null;
    private bool stopTrigger = false;
    private string cur_uuid = "UNCONNECTED";
    private iDeviceHandle deviceHandle = null;
    private LockdownClientHandle lockdownHandle = null;

    private byte[] byteBuffer;

    public static ServiceController DetectAndStartAppleDeviceService() {
        ServiceController[] scServices;
        ServiceController appleDeviceService = null;
        scServices = ServiceController.GetServices();

        foreach (ServiceController scTemp in scServices) {
            if (String.Equals(scTemp.ServiceName, "Apple Mobile Device Service", StringComparison.CurrentCultureIgnoreCase)) {
                appleDeviceService = scTemp;
                break;
            }
        }
        
        if (appleDeviceService != null) {
            if (appleDeviceService.Status != ServiceControllerStatus.Running) {
                UnityEngine.Debug.Log("[RhyLive SDK] 正在尝试启动苹果服务, 请给予程序管理员权限");
                Task.Run(async() => {
                    await Task.Delay(2000);

                    // Start Service as administrator
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "net.exe";
                    startInfo.Arguments = "start \"Apple Mobile Device Service\"";
                    startInfo.Verb = "runas";
                    process.StartInfo = startInfo;

                    process.Start();
                    process.WaitForExit();
                    process.Close();
                });
                return appleDeviceService; // need to wait and check
            } else {
                return null; // service found and running, no need to wait
            }
        } else {
            // throw new iDeviceException("找不到苹果服务, 请检查是否已安装iTunes");
            var runningProcesses = Process.GetProcesses(); // check running process for itunes
            bool foundFlag = false;
            foreach (var proc in runningProcesses) {
                if (String.Equals(proc.ProcessName, "AppleMobileDeviceProcess", StringComparison.CurrentCultureIgnoreCase) || 
                    String.Equals(proc.ProcessName, "iTunes", StringComparison.CurrentCultureIgnoreCase))
                {
                    foundFlag = true;
                    break;
                }
            }

            if (foundFlag) {
                UnityEngine.Debug.LogWarning("[RhyLive SDK] 您似乎正在使用微软商店版本iTunes, 如果连接正常, 请忽略提示, 否则请尝试前往 www.apple.com/itunes/download/win64 下载非商店版本iTunes");
                return null; // no apple service found, connect directly
            } else {
                throw new Exception("[RhyLive SDK] 没有找到苹果服务, 可能是由于您安装的是微软商店版本的iTunes, 如果是的话请启动iTunes后重试");
            }
        }
    }

    public void PollUsbConnection() {
        if (!Connected && Time.frameCount % RetryConnectionFrameInterval == 0) {
            ConnectImmdiately();
        }
    }

    public static void UsbiOS_OnDeviceChange(ref UsbmuxdEvent devEvent, IntPtr data) {
        // back to object (in callback function):
        UsbiOSReceiver receiver = GCHandle.FromIntPtr(data).Target as UsbiOSReceiver;

        switch (devEvent.@event) {
            case (int) UsbmuxdEventType.DeviceAdd:
                receiver.ConnectToDevice(devEvent.device.udid);
                break;
            case (int) UsbmuxdEventType.DeviceRemove:
                UnityEngine.Debug.Log(String.Format("Disconnected: {0}", devEvent.device.udid));
                System.Threading.Thread.Sleep(1000); // Wait for a small time to check whether unplugged device is the device in use
                if (!receiver.Connected) {
                    UnityEngine.Debug.Log("[RhyLive SDK] 您的iOS设备已经断开连接");
                }
                break;
            default:
                return;
        }
    }

    // public static void UsbiOS_OnDeviceChange(ref iDeviceEvent devEvent, IntPtr data) {
    //     // back to object (in callback function):
    //     UsbiOSReceiver receiver = GCHandle.FromIntPtr(data).Target as UsbiOSReceiver;

    //     switch (devEvent.@event)
    //     {
    //         case iDeviceEventType.DeviceAdd:
    //             receiver.ConnectToDevice(devEvent.udidString);
    //             break;
    //         case iDeviceEventType.DeviceRemove:
    //             Debug.Log(devEvent.udidString);
    //             System.Threading.Thread.Sleep(1000); // Wait for a small time to check whether unplugged device is the device in use
    //             if (!receiver.Connected) {
    //                 NotificationController.Instance.Notify("您的iOS设备已经断开连接");
    //             }
    //             break;
    //         default:
    //             return;
    //     }
    // }

    public void ConnectToDevice(String udid) {
        if (!Connected) {
            // In Update So that we can auto reconnect
            var idevice = LibiMobileDevice.Instance.iDevice;
            var lockdown = LibiMobileDevice.Instance.Lockdown;

            string deviceName = "";
            if (OpeniOSDevice(out deviceHandle, udid)) {
                if (HandshakeiOSDevice(out lockdownHandle, this.deviceHandle)) {
                    if (GetiOSDeviceName(out deviceName, this.lockdownHandle)) {
                        UnityEngine.Debug.Log(string.Format("[RhyLive SDK] iOS Device Connected: " + deviceName + ":" + udid));
                    }
                }
            }

            var error = idevice.idevice_connect(deviceHandle, this.RemotePort, out iDeviceConnectionHandle connection);

            if (error != iDeviceError.Success) {
                UnityEngine.Debug.LogError("[RhyLive SDK] 检测到了iOS设备但无法连接, 请确保应用正在运行且端口号与手机上填写一致, 并尝试重启有线连接");
            } else {
                UnityEngine.Debug.Log("[RhyLive SDK] 成功连接到iOS设备: " + deviceName);
                this.byteBuffer = new byte[2048]; // Larger than 1100 (RefDataByteCount * sizeof(float))
                this.runningiOSReceier = Task.Run(() => 
                    {
                        this.cur_uuid = udid;

                        int zero_count = 0;
                        const int zero_count_to_disconnect = 10;
                        
                        this.stopTrigger = false;
                        while (!this.stopTrigger) {
                            uint receivedBytes = 0;
                            idevice.idevice_connection_receive(connection, this.byteBuffer, (uint)this.byteBuffer.Length,
                                ref receivedBytes);
                            
                            if (receivedBytes <= 0) {
                                zero_count += 1;
                                if (receivedBytes < 0 || zero_count >= zero_count_to_disconnect) {
                                    this.stopTrigger = true;
                                }
                                continue;
                            }; 
                            this.RawUpdater(this.byteBuffer, receivedBytes); // call binded updater function
                        }
                        this.Connected = false;
                        connection.Close();
                        connection.Dispose();

                        cur_uuid = "UNCONNECTED";
                        if (lockdownHandle != null) {
                            lockdownHandle.Close();
                            lockdownHandle.Dispose();
                            lockdownHandle = null;
                        }
                        if (deviceHandle != null) {
                            deviceHandle.Close();
                            deviceHandle.Dispose();
                            deviceHandle = null;
                        }
                    }
                );
                this.Connected = true;  // Connection Success Stop Trying
            }
        }
    }

    public bool DisconnectUsb_iOS(bool forceFlag = false, string uuid = "") {
        if (forceFlag || cur_uuid.Equals(uuid)) {
            this.stopTrigger = true;
            if (this.runningiOSReceier != null) {
                this.runningiOSReceier.Wait();
                this.runningiOSReceier.Dispose();
                this.runningiOSReceier = null;
            }
            return true;
        } else {
            return false;
        }
    }

    // iOS Helper Function
    private bool OpeniOSDevice(out iDeviceHandle deviceHandle, string udid) {
        var errorRet = LibiMobileDevice.Instance.iDevice.idevice_new(out deviceHandle, udid);
        if (errorRet.IsError()) {
            UnityEngine.Debug.LogError("[RhyLive SDK] 尝试访问iOS设备失败: " + errorRet.ToString());
            return false;
        }
        return true;
    }
    private bool HandshakeiOSDevice(out LockdownClientHandle lockdownHandle, iDeviceHandle deviceHandle) {
        var errorRet = LibiMobileDevice.Instance.Lockdown.lockdownd_client_new_with_handshake(deviceHandle, out lockdownHandle, "Quamotion");
        if (errorRet.IsError()) {
            UnityEngine.Debug.LogError("[RhyLive SDK] 尝试连接iOS设备失败: " + errorRet.ToString());
            return false;
        }
        return true;
    }
    private bool GetiOSDeviceName(out string deviceName, LockdownClientHandle lockdownHandle) {
        var errorRet = LibiMobileDevice.Instance.Lockdown.lockdownd_get_device_name(lockdownHandle, out deviceName);
        if (errorRet.IsError()) {
            UnityEngine.Debug.LogError("[RhyLive SDK] 无法获取iOS设备名称: " + errorRet.ToString());
            return false;
        }
        return true;
    }

    public void ConnectImmdiately() {
        var dict = ListUsb_iOS();
        foreach (var key in dict.Keys) {
            ConnectToDevice(dict[key]);
            break;
        }
    }

    // Unused
    private Dictionary<string, string> ListUsb_iOS() {
        Dictionary<string, string> retDict = new Dictionary<string, string>();
        ReadOnlyCollection<string> udids;
        int count = 0;

        var idevice = LibiMobileDevice.Instance.iDevice;
        var lockdown = LibiMobileDevice.Instance.Lockdown;

        var ret = idevice.idevice_get_device_list(out udids, ref count);
        if (ret == iDeviceError.NoDevice) {
            // Not actually an error in our case
            return retDict;
        } else {
            if (ret.IsError()) {
                UnityEngine.Debug.LogError("[RhyLive SDK] 无法获取iOS设备列表: " + ret.ToString());
            }
        }

        // Get the device name
        foreach (var udid in udids) {
            iDeviceHandle deviceHandle;
            LockdownClientHandle lockdownHandle;
            string deviceName;

            if (OpeniOSDevice(out deviceHandle, udid)) {
                if (HandshakeiOSDevice(out lockdownHandle, deviceHandle)) {
                    if (GetiOSDeviceName(out deviceName, lockdownHandle)) {
                        retDict.Add(deviceName, udid);
                        UnityEngine.Debug.Log(String.Format("{0}: {1}", udid, deviceName));
                    }
                    lockdownHandle.Dispose();
                }
                deviceHandle.Dispose(); 
            }
        }

        return retDict;
    }
}

}