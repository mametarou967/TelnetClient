﻿using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using TelnetClient.Model.Common;
using TelnetClient.Model.Logging;
using TelnetClient.Model.SerialInterfaceProtocol;
using TelnetClient.Model.SerialPortManager;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace TelnetClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "TelnetClient";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        IEventAggregator _ea;
        LogWriter logWriter;

        public YoukyuuOutouKekka YoukyuuOutouKekka
        {
            get => serialInterfaceProtocolManager.YoukyuuOutouKekka;
            set { serialInterfaceProtocolManager.YoukyuuOutouKekka = value; }
        }

        public NinshouJoutai NinshouJoutai
        {
            get => serialInterfaceProtocolManager.NinshouJoutai;
            set { serialInterfaceProtocolManager.NinshouJoutai = value; }
        }

        public NinshouKanryouJoutai NinshouKanryouJoutai
        {
            get => serialInterfaceProtocolManager.NinshouKanryouJoutai;
            set { serialInterfaceProtocolManager.NinshouKanryouJoutai = value; }
        }

        public NinshouKekkaNgShousai NinshouKekkaNgShousai
        {
            get => serialInterfaceProtocolManager.NinshouKekkaNgShousai;
            set { serialInterfaceProtocolManager.NinshouKekkaNgShousai = value; }
        }

        public string NinshouJoutaiYoukyuuOutouRiyoushaId
        {
            get => serialInterfaceProtocolManager.RiyoushaId;
            set { serialInterfaceProtocolManager.RiyoushaId = value; }
        }

        public MainWindowViewModel(IEventAggregator ea)
        {
            serialInterfaceProtocolManager = new TelnetClientManager(new LogWriteRequester(ea));
            // コマンドの準備
            SerialStartButton = new DelegateCommand(SerialStartButtonExecute);
            SerialStopButton = new DelegateCommand(SerialStopButtonExecute);
            NinshouYoukyuuCommandTestSendButton = new DelegateCommand(NinshouYoukyuuCommandTestSendButtonExecute);
            NinshouJoutaiYoukyuuCommandTestSendButton = new DelegateCommand(NinshouJoutaiYoukyuuCommandTestSendButtonExecute);
            LogClearButton = new DelegateCommand(LogClearButtonExecute);
            PortListSelectionChanged = new DelegateCommand<object[]>(PortListChangedExecute);
            IncrementYoukyuuOutouJikanMsCommand = new DelegateCommand(IncrementYoukyuuOutouJikanMsValueExecute);
            DecrementYoukyuuOutouJikanMsCommand = new DelegateCommand(DecrementYoukyuuOutouJikanMsValueExecute);
            IncrementYoukyuuJoutaiOutouJikanMsCommand = new DelegateCommand(IncrementYoukyuuJoutaiOutouJikanMsValueExecute);
            DecrementYoukyuuJoutaiOutouJikanMsCommand = new DelegateCommand(DecrementYoukyuuJoutaiOutouJikanMsValueExecute);

            // コンボボックスの準備
            SerialPortManager.GetAvailablePortNames().ForEach(serialPort =>
            {
                if (!string.IsNullOrEmpty(serialPort))
                {
                    _serialPortList.Add(new ComboBoxViewModel(Common.ExtractNumber(serialPort), serialPort));
                }
            });

            _ea = ea;
            logWriter = new LogWriter(_ea, (Log log) =>
             {
                 SolidColorBrush solidColorBrush = new SolidColorBrush(Colors.Black);

                 if (log.logLevel == LogLevel.Error) solidColorBrush = new SolidColorBrush(Colors.Red);
                 else if (log.logLevel == LogLevel.Warning) solidColorBrush = new SolidColorBrush(Colors.DarkOrange);
                 else if (log.logLevel == LogLevel.Info) solidColorBrush = new SolidColorBrush(Colors.MediumBlue);
                 else if (log.logLevel == LogLevel.Debug) solidColorBrush = new SolidColorBrush(Colors.Gray);

                 _logItems.Add(
                         new LogItem()
                         {
                             Timestamp = log.dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                             Content = log.content,
                             ForegroundColor = solidColorBrush
                         });
             });
        }

        /// IPアドレス
        private string _ipAddress;

        public string IPAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// ボタン関係
        /// シリアル通信関係
        public DelegateCommand SerialStartButton { get; }

        private void SerialStartButtonExecute()
        {
            serialInterfaceProtocolManager.TelnetStart(IPAddress);
        }

        public DelegateCommand SerialStopButton { get; }

        private void SerialStopButtonExecute()
        {
            serialInterfaceProtocolManager.ComStop();
        }

        public DelegateCommand NinshouYoukyuuCommandTestSendButton { get; }

        // 認証要求関係

        private string _ninshouYoukyuuRiyoushaId = "00043130";

        public string NinshouYoukyuuRiyoushaId
        {
            get { return _ninshouYoukyuuRiyoushaId; }
            set { SetProperty(ref _ninshouYoukyuuRiyoushaId, value); }
        }

        private void NinshouYoukyuuCommandTestSendButtonExecute()
        {
            serialInterfaceProtocolManager.Send(new NinshouYoukyuuCommand(1, NyuutaishitsuHoukou.Nyuushitsu, _ninshouYoukyuuRiyoushaId));
        }

        public DelegateCommand NinshouJoutaiYoukyuuCommandTestSendButton { get; }

        private void NinshouJoutaiYoukyuuCommandTestSendButtonExecute()
        {
            serialInterfaceProtocolManager.Send(new NinshouJoutaiYoukyuuCommand(1, NyuutaishitsuHoukou.Nyuushitsu));
        }

        public DelegateCommand LogClearButton { get; }

        private void LogClearButtonExecute()
        {
            LogItems = new ObservableCollection<LogItem>();
        }

        public bool LogScroll
        {
            get { return logWriter.LogUpdatedEventFlag; }
            set { logWriter.LogUpdatedEventFlag = value; }
        }

        /// Combo Box
        private ObservableCollection<ComboBoxViewModel> _serialPortList =
            new ObservableCollection<ComboBoxViewModel>();

        public ObservableCollection<ComboBoxViewModel> SerialPortList
        {
            get { return _serialPortList; }
            set { SetProperty(ref _serialPortList, value); }
        }

        public DelegateCommand<object[]> PortListSelectionChanged { get; }

        private void PortListChangedExecute(object[] selectedItems)
        {
            try
            {
                var selectedItem = selectedItems[0] as ComboBoxViewModel;
                selectedSerialComPort = selectedItem.DisplayValue;
            }
            catch { }
        }

        TelnetClientManager serialInterfaceProtocolManager;
        String selectedSerialComPort = "";

        ObservableCollection<LogItem> _logItems =
            new ObservableCollection<LogItem>();
        public ObservableCollection<LogItem> LogItems
        {
            get { return _logItems; }
            set { SetProperty(ref _logItems, value); }
        }

        // 認証要求応答有効
        public bool IsResponseEnableYoukyuuOutou
        {
            get { return serialInterfaceProtocolManager.IsResponseEnableYoukyuuOutou; }
            set { serialInterfaceProtocolManager.IsResponseEnableYoukyuuOutou = value; }
        }

        // 認証要求応答時間
        public uint YoukyuuOutouJikanMs
        {
            get { return serialInterfaceProtocolManager.YoukyuuOutouJikanMs; }
            set { SetProperty(ref serialInterfaceProtocolManager.YoukyuuOutouJikanMs, value); }
        }

        public DelegateCommand IncrementYoukyuuOutouJikanMsCommand { get; private set; }
        public DelegateCommand DecrementYoukyuuOutouJikanMsCommand { get; private set; }

        private void IncrementYoukyuuOutouJikanMsValueExecute()
        {
            if (YoukyuuOutouJikanMs <= 9900)
            {
                YoukyuuOutouJikanMs = YoukyuuOutouJikanMs + 100;
            }
        }

        private void DecrementYoukyuuOutouJikanMsValueExecute()
        {
            if (YoukyuuOutouJikanMs >= 100)
            {
                YoukyuuOutouJikanMs = YoukyuuOutouJikanMs - 100;
            }
        }

        // 認証要求応答BCCエラー
        public bool IsBccErrorYoukyuuOutou
        {
            get { return serialInterfaceProtocolManager.IsBccErrorYoukyuuOutou; }
            set { serialInterfaceProtocolManager.IsBccErrorYoukyuuOutou = value; }
        }

        // 認証要求応答ID端末アドレスエラー
        public bool IsIdtAdrErrorYoukyuuOutou
        {
            get { return serialInterfaceProtocolManager.IsIdtAdrErrorYoukyuuOutou; }
            set { serialInterfaceProtocolManager.IsIdtAdrErrorYoukyuuOutou = value; }
        }

        // 認証要求応答入退室方向エラー
        public bool IsInoutDirErrorYoukyuuOutou
        {
            get { return serialInterfaceProtocolManager.IsInoutDirErrorYoukyuuOutou; }
            set { serialInterfaceProtocolManager.IsInoutDirErrorYoukyuuOutou = value; }
        }

        // 認証要求応答利用者IDエラー
        public bool IsRiyoushaIdErrorYoukyuuOutou
        {
            get { return serialInterfaceProtocolManager.IsRiyoushaIdErrorYoukyuuOutou; }
            set { serialInterfaceProtocolManager.IsRiyoushaIdErrorYoukyuuOutou = value; }
        }

        // -------------------------------------------------------------------------------

        // 認証状態要求応答有効
        public bool IsResponseEnableYoukyuuJoutaiOutou
        {
            get { return serialInterfaceProtocolManager.IsResponseEnableYoukyuuJoutaiOutou; }
            set { serialInterfaceProtocolManager.IsResponseEnableYoukyuuJoutaiOutou = value; }
        }

        // 要求状態応答時間
        public uint YoukyuuJoutaiOutouJikanMs
        {
            get { return serialInterfaceProtocolManager.YoukyuuJoutaiOutouJikanMs; }
            set { SetProperty(ref serialInterfaceProtocolManager.YoukyuuJoutaiOutouJikanMs, value); }
        }

        public DelegateCommand IncrementYoukyuuJoutaiOutouJikanMsCommand { get; private set; }
        public DelegateCommand DecrementYoukyuuJoutaiOutouJikanMsCommand { get; private set; }

        private void IncrementYoukyuuJoutaiOutouJikanMsValueExecute()
        {
            if (YoukyuuJoutaiOutouJikanMs <= 9900)
            {
                YoukyuuJoutaiOutouJikanMs = YoukyuuJoutaiOutouJikanMs + 100;
            }
        }

        private void DecrementYoukyuuJoutaiOutouJikanMsValueExecute()
        {
            if (YoukyuuJoutaiOutouJikanMs >= 100)
            {
                YoukyuuJoutaiOutouJikanMs = YoukyuuJoutaiOutouJikanMs - 100;
            }
        }


        // 認証状態要求応答ID端末アドレスエラー
        public bool IsIdtAdrErrorYoukyuuJoutaiOutou
        {
            get { return serialInterfaceProtocolManager.IsIdtAdrErrorYoukyuuJoutaiOutou; }
            set { serialInterfaceProtocolManager.IsIdtAdrErrorYoukyuuJoutaiOutou = value; }
        }

        // 認証状態要求応答入退室方向エラー
        public bool IsInoutDirErrorYoukyuuJoutaiOutou
        {
            get { return serialInterfaceProtocolManager.IsInoutDirErrorYoukyuuJoutaiOutou; }
            set { serialInterfaceProtocolManager.IsInoutDirErrorYoukyuuJoutaiOutou = value; }
        }

        // 認証要求応答利用者IDエラー
        public bool IsRiyoushaIdErrorYoukyuuJoutaiOutou
        {
            get { return serialInterfaceProtocolManager.IsRiyoushaIdErrorYoukyuuJoutaiOutou; }
            set { serialInterfaceProtocolManager.IsRiyoushaIdErrorYoukyuuJoutaiOutou = value; }
        }

        // 認証状態要求応答BCCエラー
        public bool IsBccErrorYoukyuuJoutaiOutou
        {
            get { return serialInterfaceProtocolManager.IsBccErrorYoukyuuJoutaiOutou; }
            set { serialInterfaceProtocolManager.IsBccErrorYoukyuuJoutaiOutou = value; }
        }
    }
}
