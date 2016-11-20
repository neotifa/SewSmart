﻿using System;
using BLEServiceBrowser.Model;
using BLEServiceBrowser.Utils;
using BLEServiceBrowser.Viewmodel;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Devices.Bluetooth;

namespace BLEServiceBrowser
{
    public sealed partial class MainPage : Page
    {
        // private reference to the Main viewmodel
        private MainViewModel viewmodel;

        #region BalaKrishnaRagala 
        private DeviceWatcher deviceWatcher = null;
        private DeviceInformationDisplay DeviceInfoConnected = null;

        //Handlers for device detection
        private TypedEventHandler<DeviceWatcher, DeviceInformation> handlerAdded = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerUpdated = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerRemoved = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerEnumCompleted = null;

        private DeviceWatcher blewatcher = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformation> OnBLEAdded = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> OnBLEUpdated = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> OnBLERemoved = null;

        TaskCompletionSource<string> providePinTaskSrc;
        TaskCompletionSource<bool> confirmPinTaskSrc;

        private enum MessageType { YesNoMessage, OKMessage };

        public ObservableCollection<DeviceInformationDisplay> ResultCollection
        {
            get;
            private set;
        }

        #endregion

        public MainPage()
        {
            this.InitializeComponent();

            // Set the reference to the current datacontext as this would have already been initialised.
            this.viewmodel = this.DataContext as MainViewModel;

            ResultCollection = new ObservableCollection<DeviceInformationDisplay>();

            StartWatcher();
        }

        // Handle selection changed event from the combo box 
        private void DeviceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceInformation selected = this.resultsListView.SelectedItem as DeviceInformation;


            if (selected != null)
            {
                // Send reference of selected item to the viewmodel to be processed.
                this.viewmodel.GetSelectedProfile(selected);
            }
        }

        // Handle the changed event of the supported device types.
        private void SupportedTypesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NamedGuid selected = this.DeviceSelector.SelectedItem as NamedGuid;
            if (selected != null)
            {
                // Send reference of selected item to the viewmodel to be processed.
                this.viewmodel.SetSelectedDevice(selected);
            }
            else
            {
                MessageHelper.DisplayBasicMessage(StringResources.DeviceUnAvailable);
            }

        }

        // Handle the button pressed event, this calls the viewmodel to scan for devices.
        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            this.viewmodel.GetPossibleDevices();
        }

        // Handle the user selecting a service
        private void ServiceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NamedGuid selected = this.possibleServiceList.SelectedItem as NamedGuid;

            if (selected != null)
            {
                // Send reference of selected item to the viewmodel to be processed.
                this.viewmodel.HandleSelectedService(selected);
            }
        }

        // Handle the user selecting a characteristic
        private void CharacteristicsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = this.possibleCharacteristicsList.SelectedIndex;

            if (index >= 0)
            {
                // Send index value of selected item to the viewmodel to be processed.
                viewmodel.HandleSelectedCharacteristic(index);
            }
        }

        // Handle the button pressed event, for a read function from the device.
        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            viewmodel.HandleReadCharateristic();
        }

        // Handle the button pressed event, for a write function to the device, sending the value of a text box as a string parameter.
        private void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            // get the string value from a text box
            string input = this.textbox.Text;
            // If the text box has a vlaue, perform the function, otherwise display a message box telling user the text box needs a value.
            if (!string.IsNullOrEmpty(input))
            {
                viewmodel.HandleWriteCharateristic(input);
            }
            else
            {
                MessageHelper.DisplayBasicMessage(StringResources.EmptyStringError);
            }
        }

        // Handel notification toggle changed event, and pass to the viewmodel to be processed
        private void Notification_Toggled(object sender, RoutedEventArgs e)
        {
            this.viewmodel.HandleNotificationCharateristic(this.notoficationToggle.IsOn);
        }

        // Handel indicate toggle changed event, and pass to the viewmodel to be processed
        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            this.viewmodel.HandleIndicateCharateristic(this.notoficationToggle.IsOn);
        }


        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            DeviceInformationDisplay deviceInfoDisp = resultsListView.SelectedItem as DeviceInformationDisplay;

            if (deviceInfoDisp != null)
            {
                PairButton.IsEnabled = false;
                bool paired = true;
                //if (deviceInfoDisp.IsPaired != true)
                //{
                    paired = false;
                    DevicePairingKinds ceremoniesSelected = DevicePairingKinds.ConfirmOnly | DevicePairingKinds.DisplayPin | DevicePairingKinds.ProvidePin | DevicePairingKinds.ConfirmPinMatch;
                    DevicePairingProtectionLevel protectionLevel = DevicePairingProtectionLevel.Default;

                    // Specify custom pairing with all ceremony types and protection level EncryptionAndAuthentication
                    DeviceInformationCustomPairing customPairing = deviceInfoDisp.DeviceInformation.Pairing.Custom;

                    customPairing.PairingRequested += PairingRequestedHandler;
                    DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);
                    customPairing.PairingRequested -= PairingRequestedHandler;

                    if (result.Status == DevicePairingResultStatus.Paired)
                    {
                        paired = true;
                    }
                    else
                    {
                        //UserOut.Text = "Pairing Failed " + result.Status.ToString();
                    }
                    //UpdatePairingButtons();
                //}

                if (paired)
                {
                    // device is paired, set up the sensor Tag            
                    //UserOut.Text = "Setting up SensorTag";

                    //DeviceInfoConnected = deviceInfoDisp;

                    //Start watcher for Bluetooth LE Services
                    //StartBLEWatcher();
                }
                PairButton.IsEnabled = true;
            }
        }

        private async void PairingRequestedHandler(
            DeviceInformationCustomPairing sender,
            DevicePairingRequestedEventArgs args)
        {
            switch (args.PairingKind)
            {
                case DevicePairingKinds.ConfirmOnly:
                    // Windows itself will pop the confirmation dialog as part of "consent" if this is running on Desktop or Mobile
                    // If this is an App for 'Windows IoT Core' where there is no Windows Consent UX, you may want to provide your own confirmation.
                    args.Accept();
                    break;

                case DevicePairingKinds.DisplayPin:
                    // We just show the PIN on this side. The ceremony is actually completed when the user enters the PIN
                    // on the target device. We automatically except here since we can't really "cancel" the operation
                    // from this side.
                    args.Accept();

                    // No need for a deferral since we don't need any decision from the user
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ShowPairingPanel(
                            "Please enter this PIN on the device you are pairing with: " + args.Pin,
                            args.PairingKind);

                    });
                    break;

                case DevicePairingKinds.ProvidePin:
                    // A PIN may be shown on the target device and the user needs to enter the matching PIN on 
                    // this Windows device. Get a deferral so we can perform the async request to the user.
                    var collectPinDeferral = args.GetDeferral();

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        string pin = await GetPinFromUserAsync();
                        if (!string.IsNullOrEmpty(pin))
                        {
                            args.Accept(pin);
                        }

                        collectPinDeferral.Complete();
                    });
                    break;

                case DevicePairingKinds.ConfirmPinMatch:
                    // We show the PIN here and the user responds with whether the PIN matches what they see
                    // on the target device. Response comes back and we set it on the PinComparePairingRequestedData
                    // then complete the deferral.
                    var displayMessageDeferral = args.GetDeferral();

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        bool accept = await GetUserConfirmationAsync(args.Pin);
                        if (accept)
                        {
                            args.Accept();
                        }

                        displayMessageDeferral.Complete();
                    });
                    break;
            }
        }

        private void ShowPairingPanel(string text, DevicePairingKinds pairingKind)
        {
            pairingPanel.Visibility = Visibility.Collapsed;
            pinEntryTextBox.Visibility = Visibility.Collapsed;
            okButton.Visibility = Visibility.Collapsed;
            yesButton.Visibility = Visibility.Collapsed;
            noButton.Visibility = Visibility.Collapsed;
            pairingTextBlock.Text = text;

            switch (pairingKind)
            {
                case DevicePairingKinds.ConfirmOnly:
                case DevicePairingKinds.DisplayPin:
                    // Don't need any buttons
                    break;
                case DevicePairingKinds.ProvidePin:
                    pinEntryTextBox.Text = "";
                    pinEntryTextBox.Visibility = Visibility.Visible;
                    okButton.Visibility = Visibility.Visible;
                    break;
                case DevicePairingKinds.ConfirmPinMatch:
                    yesButton.Visibility = Visibility.Visible;
                    noButton.Visibility = Visibility.Visible;
                    break;
            }

            pairingPanel.Visibility = Visibility.Visible;
        }

        private void HidePairingPanel()
        {
            pairingPanel.Visibility = Visibility.Collapsed;
            pairingTextBlock.Text = "";
        }

        private async Task<string> GetPinFromUserAsync()
        {
            HidePairingPanel();
            CompleteProvidePinTask(); // Abandon any previous pin request.

            ShowPairingPanel(
                "Please enter the PIN shown on the device you're pairing with",
                DevicePairingKinds.ProvidePin);

            providePinTaskSrc = new TaskCompletionSource<string>();

            return await providePinTaskSrc.Task;
        }

        // If pin is not provided, then any pending pairing request is abandoned.
        private void CompleteProvidePinTask(string pin = null)
        {
            if (providePinTaskSrc != null)
            {
                providePinTaskSrc.SetResult(pin);
                providePinTaskSrc = null;
            }
        }

        private async Task<bool> GetUserConfirmationAsync(string pin)
        {
            HidePairingPanel();
            CompleteConfirmPinTask(false); // Abandon any previous request.

            ShowPairingPanel(
                "Does the following PIN match the one shown on the device you are pairing?: " + pin,
                DevicePairingKinds.ConfirmPinMatch);

            confirmPinTaskSrc = new TaskCompletionSource<bool>();

            return await confirmPinTaskSrc.Task;
        }

        // If pin is not provided, then any pending pairing request is abandoned.
        private void CompleteConfirmPinTask(bool accept)
        {
            if (confirmPinTaskSrc != null)
            {
                confirmPinTaskSrc.SetResult(accept);
                confirmPinTaskSrc = null;
            }
        }

        private void StartWatcher()
        {
            string aqsFilter;

            ResultCollection.Clear();

            // Request the IsPaired property so we can display the paired status in the UI
            string[] requestedProperties = { "System.Devices.Aep.IsPaired" };

            //for bluetooth LE Devices
            //aqsFilter = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"";
            Debug.WriteLine(BluetoothDevice.GetDeviceSelectorFromPairingState(false));
            aqsFilter = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a263b43f0f974}\"";
            try {
                deviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.All);
                    
                    //BluetoothDevice.GetDeviceSelectorFromPairingState(false),
                    //requestedProperties,
                    //DeviceInformationKind.AssociationEndpoint
                    //);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            // Hook up handlers for the watcher events before starting the watcher

            handlerAdded = new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    ResultCollection.Add(new DeviceInformationDisplay(deviceInfo));
                });
            });
            deviceWatcher.Added += handlerAdded;

            handlerUpdated = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    // Find the corresponding updated DeviceInformation in the collection and pass the update object
                    // to the Update method of the existing DeviceInformation. This automatically updates the object
                    // for us.
                    foreach (DeviceInformationDisplay deviceInfoDisp in ResultCollection)
                    {
                        if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            deviceInfoDisp.Update(deviceInfoUpdate);
                            break;
                        }
                    }
                });
            });
            deviceWatcher.Updated += handlerUpdated;



            handlerRemoved = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    // Find the corresponding DeviceInformation in the collection and remove it
                    foreach (DeviceInformationDisplay deviceInfoDisp in ResultCollection)
                    {
                        if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            ResultCollection.Remove(deviceInfoDisp);
                            break;
                        }
                    }
                });
            });
            deviceWatcher.Removed += handlerRemoved;

            handlerEnumCompleted = new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    string dbg = "Found " + ResultCollection.Count.ToString() + " Bluetooth LE Devices";
                    Debug.WriteLine(dbg);
                    resultsListView.ItemsSource = ResultCollection;
                });
            });

            deviceWatcher.EnumerationCompleted += handlerEnumCompleted;

            deviceWatcher.Start();
        }

        private void StopWatcher()
        {
            if (null != deviceWatcher)
            {
                // First unhook all event handlers except the stopped handler. This ensures our
                // event handlers don't get called after stop, as stop won't block for any "in flight" 
                // event handler calls.  We leave the stopped handler as it's guaranteed to only be called
                // once and we'll use it to know when the query is completely stopped. 
                deviceWatcher.Added -= handlerAdded;
                deviceWatcher.Updated -= handlerUpdated;
                deviceWatcher.Removed -= handlerRemoved;
                deviceWatcher.EnumerationCompleted -= handlerEnumCompleted;

                if (DeviceWatcherStatus.Started == deviceWatcher.Status ||
                    DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status)
                {
                    deviceWatcher.Stop();
                }
            }
        }

        private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void yesButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void noButton_Click(object sender, RoutedEventArgs e)
        {

        }

      
    }
}
