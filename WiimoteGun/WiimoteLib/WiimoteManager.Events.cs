﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WiimoteLib.DataTypes;
using WiimoteLib.Devices;
using WiimoteLib.Events;

namespace WiimoteLib {
	public static partial class WiimoteManager {
		public static event EventHandler<WiimoteDiscoveredEventArgs> Discovered;
		public static event EventHandler<WiimoteEventArgs> Connected;
		public static event EventHandler<WiimoteDisconnectedEventArgs> Disconnected;
		public static event EventHandler<WiimoteConnectionFailedEventArgs> ConnectionFailed;
		public static event EventHandler<WiimoteExceptionEventArgs> WiimoteException;
		public static event EventHandler<WiimoteExtensionEventArgs> ExtensionChanged;
		public static event EventHandler<WiimoteStateEventArgs> StateChanged;
		public static event EventHandler<WiimoteRangeEventArgs> InRange;
		public static event EventHandler<WiimoteRangeEventArgs> OutOfRange;
		public static event EventHandler<Exception> ManagerException;

		// Called by manager

		private static bool RaiseDiscovered(BluetoothDeviceInfo bt, HIDDeviceInfo hid) {
			if (bt?.IsInvalid ?? true)
				Log.Info($"{hid} Discovered");
			else
				Log.Info($"{bt} Discovered");

			WiimoteDeviceInfo device;
			//FIXME: Quick fix to support both Bluetooth and DolphinBar connections.
			if (bt?.IsInvalid ?? true)// && DolphinBarMode)
				device = new WiimoteDeviceInfo(hid, true);
			else
				device = new WiimoteDeviceInfo(bt, hid);
			WiimoteDiscoveredEventArgs e = new WiimoteDiscoveredEventArgs(device);
			Discovered?.Invoke(null, e);
			if (e.AddDevice) {
				try {
					Connect(e.Device);
				}
				catch (Exception ex) {
					RaiseConnectionFailed(e.Device, ex);
					return true;
				}
			}
			return e.KeepSearching;
		}

		private static void RaiseConnected(Wiimote wiimote) {
			Log.Info($"{wiimote} Connected");
			Connected?.Invoke(null, new WiimoteEventArgs(wiimote));
			UpdateTaskMode();
		}

		private static void RaiseConnectionFailed(WiimoteDeviceInfo device, Exception ex) {
			Log.Warning($"{device} Connection Failed: {ex.Message}");
			ConnectionFailed?.Invoke(null, new WiimoteConnectionFailedEventArgs(device, ex));
		}

		private static void RaiseDisconnected(Wiimote wiimote, DisconnectReason reason, bool? removeDevice = null) {
			Log.Info($"{wiimote} Disconnected: {reason}");
			Disconnected?.Invoke(null, new WiimoteDisconnectedEventArgs(wiimote, reason));
			wiimote.RaiseDisconnected(reason);
			if (removeDevice ?? unpairOnDisconnect)
				wiimote.Device.Bluetooth.RemoveDevice();
			UpdateTaskMode();
		}

		private static void RaiseInRange(Wiimote wiimote) {
			Log.Info($"{wiimote} In Range");
			InRange?.Invoke(null, new WiimoteRangeEventArgs(wiimote, true));
			wiimote.RaiseInRange();
		}

		private static void RaiseOutOfRange(Wiimote wiimote) {
			Log.Info($"{wiimote} Out of Range");
			OutOfRange?.Invoke(null, new WiimoteRangeEventArgs(wiimote, false));
			wiimote.RaiseOutOfRange();
		}

		private static void RaiseManagerException(Exception ex) {
			Log.Error($"Manager Exception: {ex.Message}");
			ManagerException?.Invoke(null, ex);
		}


		// Called by Wiimote

		internal static void RaiseWiimoteException(Wiimote wiimote, Exception ex) {
			Log.Error($"{wiimote} Exception: {ex}");
			WiimoteException?.Invoke(null, new WiimoteExceptionEventArgs(wiimote, ex));
		}

		internal static void RaiseExtensionChanged(Wiimote wiimote, ExtensionType type, bool inserted) {
			Log.Info($"{wiimote} Extension: {type} {(inserted ? "Inserted" : "Removed")}");
			ExtensionChanged?.Invoke(wiimote, new WiimoteExtensionEventArgs(wiimote, type, inserted));
		}

		internal static void RaiseStateChanged(Wiimote wiimote) {
			//Log.WriteLine($"{wiimote} State");
			StateChanged?.Invoke(null, new WiimoteStateEventArgs(wiimote));
		}
	}
}
