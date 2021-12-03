using System;
using System.Linq;
using System.Text;
using XiangQi.Common;
using System.IO.Ports;
using XiangQi.Log.Log4net;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;

namespace XiangQi.Ros.Screen
{
	/// <summary>
	/// 操作
	/// </summary>
	public class Operation
    {
		[DllImport("user32.dll", EntryPoint = "SetCursorPos")]
		private static extern int SetCursorPos(int x, int y);

		private static object locker = new object();
		public static Queue MessageQueue = new Queue();

		public Action<byte[]> SendMessageAction;
		readonly SerialPort ComDevice = new SerialPort();		
		private System.Timers.Timer _timer;
        InputType _inputType = 0;
		byte[] message;
		byte[] oldMessage;
		bool isMouseDown = false;
		ScreenStruct _screenStruct;
		DateTime mouseDateTime;
		int oldX=0, oldY=0;
		ComDeviceStruct _comDeviceStruct;

		readonly byte[] keyData = new byte[] { 0xE5, 0x00, 0xA3, 0x08, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
		readonly Dictionary<int, int> keyList = new Dictionary<int, int>();

        //const string SBUF = "0_0|1_|2_|3_|4_|5_|6_|7_|8_42|9_43|10_|11_|12_|13_40|14_|15_|16_|17_|18_|19_72|20_57|21_|22_|23_|24_|25_|26_|27_41|28_|29_|30_|31_|32_44|33_75|34_78|35_77|36_74|37_80|38_82|39_79|40_81|41_|42_|43_|44_70|45_73|46_76|47_|48_39|49_30|50_31|51_32|52_33|53_34|54_35|55_36|56_37|57_38|58_|59_|60_|61_|62_|63_|64_|65_4|66_5|67_6|68_7|69_8|70_9|71_10|72_11|73_12|74_13|75_14|76_15|77_16|78_17|79_18|80_19|81_20|82_21|83_22|84_23|85_24|86_25|87_26|88_27|89_28|90_29|91_左win|92_右win|93_|94_|95_|96_98|97_89|98_90|99_91|" +
        //	"100_92|101_93|102_94|103_95|104_96|105_97|106_85|107_87|108_|109_86|110_99|111_84|112_58_F1|113_59_F2|114_60_F3|115_61_F4|116_62_F5|117_63_F6|118_64_F7|119_F8|120_F9|121_F10|122_F11|123_F12|124_F13|125_71_F14|126_72_F15|127_73_F16|128_|129_|130_|131_|132_|133_|134_|135_|136_|137_|138_|139_|140_|141_|142_|143_|144_83|145_71|146_|147_|148_|149_|150_|151_|152_|153_|154_|155_|156_|157_|158_|159_|160_左shift|161_右shift|162_左ctrl|163_右ctrl|164_左alt|165_右alt|166_|167_|168_|169_|170_|171_|172_|173_|174_|175_|176_|177_|178_|179_|180_|181_|182_|183_|184_|185_|" +
        //	"186_51|187_46|188_54|189_45|190_55|191_56|192_53|193_|194_|195_|196_|197_|198_|199_|200_|201_|202_|203_|204_|205_|206_|207_|208_|209_|210_|211_|212_|213_|214_|215_|216_|217_|218_|219_47|220_49|221_48|222_52|223_|224_|225_|226_|227_|228_|229_|230_|231_|232_|233_|234_|235_|236_|237_|238_|239_|240_|241_|242_|243_|244_|245_|246_|247_|248_|249_|250_|251_|252_|253_|254_|";

        const string SBUF = "0_0|8_42|9_43" +
			"|13_40|19_72" +
			"|20_57|27_41" +
			"|32_44|33_75|34_78|35_77|36_74|37_80|38_82|39_79" +
			"|40_81|44_70|45_73|46_76|48_39|49_30" +
			"|50_31|51_32|52_33|53_34|54_35|55_36|56_37|57_38" +
			"|65_4|66_5|67_6|68_7|69_8" +
			"|70_9|71_10|72_11|73_12|74_13|75_14|76_15|77_16|78_17|79_18" +
			"|80_19|81_20|82_21|83_22|84_23|85_24|86_25|87_26|88_27|89_28" +
			"|90_29|96_98|97_89|98_90|99_91|" +
            "100_92|101_93|102_94|103_95|104_96|105_97|106_85|107_87|109_86" +
			"|110_99|111_84|112_58_F1|113_59_F2|114_60_F3|115_61_F4|116_62_F5|117_63_F6|118_64_F7|119_F8" +
			"|120_F9|121_F10|122_F11|123_F12|124_F13|125_71_F14|126_72_F15|127_73_F16" +
			"|144_83|145_71|" +
            "186_51|187_46|188_54|189_45" +
			"|190_55|191_56|192_53" +
			"|219_47|220_49|221_48|222_52";

        /// <summary>
        /// 
        /// </summary>
        public Operation()
        {
			var pos1 = SBUF.Split("|");
			foreach(var item in pos1)
            {
				var e = item.Split("_");
				if(e.Length >= 2 && int.TryParse(e[0], out int key) && int.TryParse(e[1], out int val))
					keyList.Add(key, val);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="screenStruct"></param>
		/// <param name="inputType"></param>
		/// <param name="comDeviceStruct"></param>
		public bool OperationChange(ScreenStruct screenStruct, InputType inputType, ComDeviceStruct comDeviceStruct)
        {
			_inputType = inputType;
			_screenStruct = screenStruct;
			_comDeviceStruct = comDeviceStruct;

			_timer = new System.Timers.Timer(10)
            {
                AutoReset = true
            };
            _timer.Elapsed += Timer_Elapsed;

			ComDevice.DataReceived += new SerialDataReceivedEventHandler(Com_DataReceived);//绑定事件
			if(_inputType == InputType.Disco0 || _inputType == InputType.TSK0)
            {
				return StartComDevice(comDeviceStruct);
			}
			return true;
		}

		#region COM

		bool readCom = false;

		/// <summary>
		/// 接收COM接口数据
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Com_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			Thread.Sleep(_comDeviceStruct.DelayTime);
			if (!readCom) return;

			byte[] buffer = new byte[ComDevice.BytesToRead];
			ComDevice.Read(buffer, 0, buffer.Length);//读取数据			
            if (buffer.Length > 1)
            {
				//LogHelper.Info($"COM发送信息：{string.Join(",", buffer.ToArray().Select(item => item.ToString("X2")))}");
				SendMessageAction?.Invoke(buffer);
			}				
		}

		/// <summary>
		/// 开始COM接口接收消息功能
		/// </summary>
		/// <param name="comDeviceStruct"></param>
		public bool StartComDevice(ComDeviceStruct comDeviceStruct)
		{
			if (ComDevice.IsOpen == false)
			{
				ComDevice.PortName = comDeviceStruct.PortName;
				ComDevice.BaudRate = comDeviceStruct.BaudRate;
				ComDevice.Parity = comDeviceStruct.Parity;
				ComDevice.DataBits = comDeviceStruct.DataBits;
				ComDevice.StopBits = comDeviceStruct.StopBits;
				ComDevice.ReceivedBytesThreshold = 1;
				try
				{
					ComDevice.Open();
					readCom = true;
				}
				catch (Exception ex)
				{
					LogHelper.Error(ex.Message, ex);
					readCom = false;
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		public void StopComDevice()
		{
			readCom = false;
			Thread.Sleep(2);
			try
			{
				if (ComDevice.IsOpen)
					ComDevice.Close();
			}
			catch (Exception e)
			{
				LogHelper.Error(e);
			}
		}
		#endregion

		/// <summary>
		/// 定时器发送消息
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
			var t = (DateTime.Now - mouseDateTime).TotalMilliseconds;
            if (t >= 500 && _inputType == InputType.SimulateKM_5)
            {
				SetCursorPos(_screenStruct.LocalDpiX / 2, _screenStruct.LocalDpiY / 2);
                mouseDateTime = DateTime.Now;
				oldX = 0;
				oldY = 0;
			}
            else
            {
				if (MessageQueue.Count > 0) //如果队列中有数据，将其出队列
				{
					lock (locker)
					{
						message = (byte[])MessageQueue.Dequeue();
						SendMessageAction?.Invoke(message);
					}
				}
			}
        }

		/// <summary>
		/// 开始定时器
		/// </summary>
		public void StartTimer()
		{
			switch (_inputType)
			{
				case InputType.Disco1:
				case InputType.Disco2:
				case InputType.Disco7:
					_timer.Interval = 10;					
					break;
				case InputType.Disco3:
				case InputType.TSK1:
				case InputType.TSK2:
				case InputType.Zenvoce:
					_timer.Interval = 20;
					break;
				default:
					_timer.Interval = 10;
					break;
			}
			_timer.Start();
		}

		/// <summary>
		/// 
		/// </summary>
		public void StopTimer()
        {
            try
            {
				_timer?.Stop();
				_timer?.Dispose();
			}
			catch(Exception e)
            {
				LogHelper.Error(e);
            }
		}		

        #region Mouse
        /// <summary>
        /// 开始鼠标发送
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="button"></param>
        /// <param name="delta"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void StartMouseSend(string eventType, string button, string delta, int x, int y)
        {
			//MessageQueue.Clear();
			mouseDateTime = DateTime.Now;
			switch (eventType)
            {
				case "MouseDown":
					isMouseDown = true;
					//LogHelper.Info($"MouseDown-{DateTime.Now}:{DateTime.Now.Millisecond}");
					break;
				case "MouseMove":
					//isMouseMove = true;
					if (_inputType == InputType.TSK1 || _inputType == InputType.TSK2) return;
					break;
				case "MouseUp":					
					isMouseDown = false;
					break;
				default:
					break;

			}
			message = GetMouseMessage(button ,delta, x, y);
			ValidationMouseSendMessage();
			//LogHelper.Info(message);
		}		

		/// <summary>
		/// 获取鼠标操作消息
		/// </summary>
		/// <param name="button"></param>
		/// <param name="delta"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		private byte[] GetMouseMessage(string button, string delta, int _x, int _y)
        {
			var x = Math.Floor(_x * _screenStruct.RatioX + _screenStruct.OffsetX);
			var y = Math.Floor(_y * _screenStruct.RatioY + _screenStruct.OffsetY);

			var buffer = new byte[] { };

			switch (_inputType)
            {
				case InputType.Disco0:
					//message = isMouseDown ? new byte[] { 0xE5, 0x00, 0xA5, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x94 } :
					//	new byte[] { 0xE6, 0x00, 0xA5, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x94 };
					return buffer;
				case InputType.Disco1:
					return Encoding.UTF8.GetBytes($"{(isMouseDown ? "T" : "R")}{string.Format("{0:0000}", x)},{string.Format("{0:000}", y)}\r");
				case InputType.Disco2:
				case InputType.Disco3:
					return isMouseDown ? 
						new byte[] { 0x11, (byte)Math.Floor(x / 256), (byte)Math.Floor(x % 256), (byte)Math.Floor(y / 256), (byte)Math.Floor(y % 256) } 
						: new byte[] { 0x10 };
				case InputType.TSK0:
					return buffer;
				case InputType.TSK1:
				case InputType.TSK2:
					//string.Format("{0:0000.00}", 194.039) //结果为：0194.04
					//message = Encoding.UTF8.GetBytes($"{(isMouseDown ? "T" : "R")}{x.ToString().PadLeft(4, '0')},{(_screenStruct.RemoteDpiY - y).ToString().PadLeft(4, '0')}\r");
					//LogHelper.Info(Encoding.UTF8.GetBytes($"{(isMouseDown ? "T" : "R")}{string.Format("{0:0000}", x)},{string.Format("{0:0000}", _screenStruct.RemoteDpiY - y)}\r"));
					//MessageQueue.Clear();
					buffer = Encoding.UTF8.GetBytes($"{(isMouseDown ? "T" : "R")}{string.Format("{0:0000}", x)},{string.Format("{0:0000}", _screenStruct.RemoteDpiY - y)}\r");
					if (isMouseDown)
                    {
						Task.Run(()=> {
							while (isMouseDown)
							{
								lock (locker)
								{
									MessageQueue.Enqueue(buffer);
								}
								Thread.Sleep(20);
							}
						});
					}
                    else
                    {
						lock (locker)
						{
							//MessageQueue.Clear();
							MessageQueue.Enqueue(buffer);
						}
					}
					
					return buffer;
				case InputType.SimulateKM:
				case InputType.SimulateMouse:
				case InputType.SimulateTouch:
					buffer = new byte[] { 0xE5, 0x00, 0xA5, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    buffer[5] = isMouseDown && button.Equals("Left") ? (byte)(buffer[5] | 0x01) : (byte)(buffer[5] & 0xF0);
                    buffer[5] = isMouseDown && button.Equals("Right") ? (byte)(buffer[5] | 0x02) : (byte)(buffer[5] & 0xFD);
                    buffer[5] = isMouseDown && button.Equals("Middle") ? (byte)(buffer[5] | 0x04) : (byte)(buffer[5] & 0xFB);

                    buffer[7] = (byte)Math.Floor(x / 256);
                    buffer[8] = (byte)Math.Floor(x % 256);
                    buffer[9] = (byte)Math.Floor(y / 256);
                    buffer[10] = (byte)Math.Floor(y % 256);
                    buffer[11] = (byte)Math.Floor((double)buffer.Sum(item => item) % 256);
					return buffer;
				case InputType.SimulateKM_2:
				case InputType.SimulateMouse_2:
				case InputType.SimulateTouch_2:
					return buffer;
                case InputType.Zenvoce:
					buffer = new byte[] { 0x81,
							(byte)(Math.Floor((_screenStruct.RemoteDpiY -y) / 128)+1),
							(byte)Math.Floor((_screenStruct.RemoteDpiY - y) % 128),
							(byte)(Math.Floor((_screenStruct.RemoteDpiX - x) / 128) + 1),
							(byte)Math.Floor((_screenStruct.RemoteDpiX - x) % 128) };
                    if (!isMouseDown)
                    {
						buffer[0] = 0x80;
					}
					return buffer;
				case InputType.Disco7:
					return buffer;
				case InputType.SimulateKM_5:
					buffer = new byte[] { 0xE5, 0x00, 0xA6, 0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
					buffer[5] = isMouseDown && button.Equals("Left") ? (byte)(buffer[5] | 0x01) : (byte)(buffer[5] & 0xF0);
					buffer[5] = isMouseDown && button.Equals("Right") ? (byte)(buffer[5] | 0x02) : (byte)(buffer[5] & 0xFD);
					buffer[5] = isMouseDown && button.Equals("Middle") ? (byte)(buffer[5] | 0x04) : (byte)(buffer[5] & 0xFB);
										
					int deltaX = _x - oldX;
					int deltaY = _y - oldY;
					int countX = Math.Abs(deltaX / 255);
					int countY = Math.Abs(deltaY / 255);
					int maxValue = Math.Max(countX, countY);
					for (int i = 0; i <= maxValue; i++)
					{
                        if (countX > 0)
                        {
                            if (deltaX > 0)
                            {
								buffer[7] = 0xFF;
								buffer[8] = 0x0;
							}
							else if (deltaX < 0)
                            {
								buffer[7] = 0x01;
								buffer[8] = 0xFF;
							}
                            else
                            {
								buffer[7] = 0x0;
								buffer[8] = 0x0;
							}
                        }
						else if (countX == 0)
                        {
							if (deltaX > 0)
							{
								buffer[7] = (byte)Math.Floor((double)Math.Abs(deltaX) % 255);
								buffer[8] = 0x0;
							}
							else if (deltaX < 0)
							{
								buffer[7] = (byte)(0x100-Math.Floor((double)Math.Abs(deltaX) % 255));
								buffer[8] = 0xFF;
							}
							else
							{
								buffer[7] = 0x0;
								buffer[8] = 0x0;
							}
						}

                        if (countY > 0)
                        {
                            if (deltaY > 0)
                            {
                                buffer[9] = 0xFF;
                                buffer[10] = 0x0;
                            }
                            else if (deltaY < 0)
                            {
                                buffer[9] = 0x01;
                                buffer[10] = 0xFF;
                            }
                            else
                            {
                                buffer[9] = 0x0;
                                buffer[10] = 0x0;
                            }
                        }
                        else if (countY == 0)
                        {
                            if (deltaY > 0)
                            {
                                buffer[9] = (byte)Math.Floor((double)Math.Abs(deltaY) % 255);
                                buffer[10] = 0x0;
                            }
                            else if (deltaY < 0)
                            {
                                buffer[9] = (byte)(0x100 - Math.Floor((double)Math.Abs(deltaY) % 255));
                                buffer[10] = 0xFF;
                            }
                            else
                            {
                                buffer[9] = 0x0;
                                buffer[10] = 0x0;
                            }
                        }

                        countX--;
						countY--;
					}
					buffer[11] = (byte)Math.Floor((double)buffer.Sum(item => item) % 256);
					oldX = _x;
					oldY = _y;
					return buffer;
					
				default:
					return null; //new byte[] { 0xE5, 0x00, 0xA5, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x94 };
            }
		}

		/// <summary>
		/// 验证鼠标发送消息
		/// </summary>

		private bool ValidationMouseSendMessage()
        {
			if (message == null || message.Length < 1)
			{
				return false;
			}

			bool isSend;
            switch (_inputType)
            {
				case InputType.Disco1:
				case InputType.Disco2:
				case InputType.Disco7:
				case InputType.SimulateTouch:
                    if (oldMessage == null || !Enumerable.SequenceEqual(oldMessage, message))
                    {
                        if (isMouseDown)
                        {
                            oldMessage = message;
							isSend = true;
							lock (locker)
							{
								MessageQueue.Enqueue(message);
							}
						}
                        else
                        {
                            isSend = oldMessage != null;
                            if (isSend)
                            {
								lock (locker)
								{
									MessageQueue.Enqueue(message);
								}
							}
                            oldMessage = null;
						}
                    }
                    else
                    {
						isSend = false;
					}
					break;
				case InputType.Disco3:
				case InputType.TSK1:
				case InputType.TSK2:
				case InputType.Zenvoce:
					// if (isMouseDown)
                    // {
					// 	oldMessage = message;
					// 	isSend = true;
					// }
                    // else
                    // {
					// 	isSend = oldMessage != null;
					// 	oldMessage = null;
					// }
					isSend = false;
					break;				
				default:
					isSend = oldMessage == null || !Enumerable.SequenceEqual(oldMessage, message);
					if(isSend)
						lock (locker)
						{
							MessageQueue.Enqueue(message);
						}
					oldMessage = message;
					break;
            }			
			return isSend;
		}

        #endregion

        #region Keyboard

		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="keyCode"></param>
		public void StartKeyboardSend(string eventType, int keyValue)
        {
			if (_inputType != InputType.Disco0 && _inputType != InputType.TSK0)
            {
				var buffer = GetKeyboardMessage(eventType, keyValue);
				SendMessageAction?.Invoke(buffer);
			}				
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="keyCode"></param>
		/// <returns></returns>
		private byte[] GetKeyboardMessage(string eventType, int keyValue)
        {			
			switch (keyValue)
			{
				case 92: // "RWin":
					keyData[4] = eventType.Equals("KeyDown") ? (byte)(keyData[4] | 0x80) : (byte)(keyData[4] & 0x7F);
					break;
				case 91: // "LWin":												  
					keyData[4] = eventType.Equals("KeyDown") ? (byte)(keyData[4] | 0x8) : (byte)(keyData[4] & 0xF7);
					break;
				case 163: // "RControlKey":											  
					keyData[4] = eventType.Equals("KeyDown") ? (byte)(keyData[4] | 0x10) : (byte)(keyData[4] & 0xEF);
					break;
				case 162:// "LControlKey":											  
					keyData[4] = eventType.Equals("KeyDown") ? (byte)(keyData[4] | 0x1) : (byte)(keyData[4] & 0xF0);
					break;
				case 161:// "RShiftKey":											  
					keyData[4] = eventType.Equals("KeyDown") ? (byte)(keyData[4] | 0x20) : (byte)(keyData[4] & 0xDF);
					break;
				case 160:// "LShiftKey":											  
					keyData[4] = eventType.Equals("KeyDown") ? (byte)(keyData[4] | 0x2) : (byte)(keyData[4] & 0xFD);
					break;
				case 165:// "LMenu":
				case 164:// "RMenu":												  
					keyData[4] = eventType.Equals("KeyDown") ? (byte)(keyData[4] | 0x4) : (byte)(keyData[4] & 0xFB);
					break;
				default:
					for (int i = 6; i < 11; i++)
					{
						if (eventType.Equals("KeyDown"))
						{
							if (keyData[i] == 0x00)
							{
								if(keyList.ContainsKey(keyValue))
									keyData[i] = (byte)keyList[keyValue]; //(byte)keyList[keyCode].ToString();
								break;
							}
							else if (keyList.ContainsKey(keyValue) && keyData[i] == keyList[keyValue])
							{
								break;
							}
						}
						else
						{
							if (keyList.ContainsKey(keyValue) && keyData[i] == keyList[keyValue])
							{
								keyData[i] = 0x00;
								break;
							}
							keyData[i] = 0x00;
						}
					}
					break;
			}
			
			return keyData;
        }
		#endregion
	}
}
