using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace DisusedCOMBulkDeleterCS{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window {

		// デバイス情報格納
		ObservableCollection<DevInfo> listDevInfo = new ObservableCollection<DevInfo>();

		public MainWindow() {
			InitializeComponent();

			// ダミーデータ作成
			listDevInfo.Add(new DevInfo { instanceId = "", devExplain = "", comNumber = "", });

			// データグリッドにデータを設定
			dataGrid.ItemsSource = listDevInfo;

			// データグリッドのカラム幅を自動調整
			dataGrid.AutoGeneratingColumn += (s, e) => {
				if (e.PropertyType == typeof(string)) {
					e.Column.Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
				}
			};

			ContentRendered += (s, e) => { GetListCOM(); };
		}

		public bool GetListCOM() {
			listDevInfo.Clear();
			string output;
			ProcessStartInfo startInfo = new ProcessStartInfo {
				FileName = @"c:\windows\sysnative\pnputil.exe",
				Arguments = "/enum-devices /class Ports",
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};
			try {
				using (Process proc = Process.Start(startInfo)) {
					output = proc.StandardOutput.ReadToEnd();
					proc.WaitForExit();
				}
				//Console.WriteLine($"Output:{output}");
			}
			catch (Exception ex){
				//Console.WriteLine($"Error:{ex.Message}");
				return true;
			}

			string pattern = @"インスタンス ID:\s+(.+?)\s*\r\nデバイスの説明:\s+(.+?)\s*\(COM(\d+?)\)\s*\r\n";

			MatchCollection matches = Regex.Matches(output, pattern, RegexOptions.Singleline);
			//Console.WriteLine(matches.Count);
			foreach (Match match in matches) {
				if (match.Groups.Count == 4) {	// 認識結果が「全体、インスタントID、デバイスの説明、COM番号」の4要素から成っているとき=>値に欠けが無いとき。
					string instanceId = match.Groups[1].Value;
					string devExplain = match.Groups[2].Value;
					string comNumber = match.Groups[3].Value;
					//Console.WriteLine($"{instanceId}:{devExplain}:{comNumber}");
					listDevInfo.Add(new DevInfo {
						instanceId	= instanceId,
						devExplain	= devExplain,
						comNumber	= comNumber,
					});
				}
			}

			return false;
		}

		private void clk_btn_scan(object sender, RoutedEventArgs e) { GetListCOM(); }


		public bool DeleteCOMCore(List<DevInfo> dstDevInfo ) {

			List<DevInfo> selectedDev = new List<DevInfo>();

			// 選択されたアイテムを処理する（例：コンソールに表示）
			foreach (DevInfo device in dstDevInfo) {
				//Console.WriteLine($"InstanceId: {device.instanceId}, Explanation: {device.devExplain}, COM: {device.comNumber}");
				selectedDev.Add(device);
			}
			foreach (DevInfo device in selectedDev) {
				ProcessStartInfo startInfo = new ProcessStartInfo {
					FileName = @"c:\windows\sysnative\pnputil.exe",
					Arguments = $"/remove-device {device.instanceId}",
					UseShellExecute = false,
				};
				try {
					using (Process.Start(startInfo)) { }
				}
				catch (Exception ex) {
					//Console.WriteLine($"Error:{ex.Message}");
					return true;
				}
				listDevInfo.Remove(device);
			}
			return false;
		}

		private void clk_btn_delete(object sender, RoutedEventArgs e) {
			DeleteCOMCore(dataGrid.SelectedItems.Cast<DevInfo>().ToList());
		}

		private void clk_btn_delete_all(object sender, RoutedEventArgs e) {
			DeleteCOMCore(listDevInfo.Cast<DevInfo>().ToList());
		}
	}

	public class DevInfo : INotifyPropertyChanged{
		public event PropertyChangedEventHandler PropertyChanged;
		private string _instanceId;
		private string _devExplain;
		private string _comNumber;

		public string instanceId {
			get { return _instanceId; }
			set {
				_instanceId = value;
				OnPropertyChanged("インスタンス ID");
			}
		}
		public string devExplain {
			get { return _devExplain; }
			set {
				_devExplain = value;
				OnPropertyChanged("デバイスの説明");
			}
		}
		public string comNumber {
			get { return _comNumber; }
			set {
				_comNumber = value;
				OnPropertyChanged("COM番号");
			}
		}

		protected void OnPropertyChanged(string propertyName) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
