using BoxAgr.BLL.Events;
using BoxAgr.BLL.Interfaces;
using NModbus;
using Peripherals;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Util;

namespace BoxAgr.BLL.Controllers
{
    public class SlaveModBusKerchController: IBusControl
    {

        private readonly IModBusState _modBusState;
        private int _numberOfConnections;
      
        public event BoxInPositionHandler? BoxInPosition;
        public event PowerLossHandler? PowerLoss;
        public event SessionStateEvent? StatusChange; // событие изменение статуса.

        private readonly bool[] _inputs = new bool[8];
        private readonly bool[] _outputs = new bool[8];

        private readonly ushort _inStartAddress;
        private readonly ushort _outStartAddress;
        private readonly byte _slaveAddress;

        public bool IsBoxInPosition { get { return _inputs[0]; } }
        public bool IsPowerLoss { get { return _inputs[1]; } }
        public bool IsGreenLightActive { get { return _outputs[2]; } }

        public string IpAddress { get; }

        private readonly AutoResetEvent _waitEvent = new(false); //евент ожидания между циклами опроса

        public SlaveModBusKerchController( IModBusState modBusState, string ip,byte slaveAddress, ushort inStartAddress, ushort outStartAddress ) 
        {
            _modBusState = modBusState;
            _inStartAddress = inStartAddress;
            _outStartAddress = outStartAddress;
            IpAddress = ip;
            _slaveAddress = slaveAddress;
        }


        public void Start(CancellationToken cancelationToken)
        {
            Task.Run(async () =>
            {
                ushort _numCoils = (ushort)_inputs.Length;
               
                while (!cancelationToken.IsCancellationRequested)
                {
                    try
                    {
                        using (TcpClient client = new TcpClient(IpAddress, 502))
                        {
                            var factory = new ModbusFactory();
                            using IModbusMaster _modbusMaster = factory.CreateMaster(client);
                           
                            StatusChange?.Invoke(2, PeripheralsType.Scanner, SessionStates.OnLine);
                            Log.Write("SMB", $"Соединение с сервером модбаса {IpAddress} установлено", EventLogEntryType.Information, 20);

                            while (client.Connected)
                            {

                                // read five input values
                                ushort[] ins = _modbusMaster.ReadHoldingRegisters(_slaveAddress, _inStartAddress, 1);
                                bool[] inputs = UshortToBoolArray(ins[0]);

                                // write three coils
                                ushort[] ushorts = BoolArratyToUshortArray(_outputs);
                                _modbusMaster.WriteMultipleRegisters(_slaveAddress, _outStartAddress, ushorts);

                                //events
                                if (inputs[0] != _inputs[0])
                                {
                                    
                                    _modBusState.IsBoxSet = inputs[0];
                                    _modBusState.Diagnostic = _modBusState.IsBoxSet ? "Получено подтвержденее выпуска короба" : " ";

                                    BoxInPosition?.Invoke(this, inputs[0]);
                                }

                                if (inputs[1] != _inputs[1])
                                {
                                    _modBusState.Power = inputs[1];
                                    PowerLoss?.Invoke(this, inputs[1]);
                                }

                                //save
                                Array.Copy(inputs, 0, _inputs, 0, _numCoils);

                                //минимальное время между циклами опроса
                                await Task.Delay(50);

                                //wait event or timeout
                                _waitEvent.WaitOne(250);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write("SMB", ex.ToString(), EventLogEntryType.Information, 21);
                    }

                    Log.Write("SMB", $"Соединение с сервером модбаса {IpAddress} потеряно", EventLogEntryType.Information, 22);
                    StatusChange?.Invoke(2, PeripheralsType.Scanner, SessionStates.Disconnect);
                    await Task.Delay(500);
                }
                return true;
            }).ConfigureAwait(false);
        }


        public Task<bool> StartScan()
        {
            return Task.Run(async () =>
            {
                _outputs[0] = true;
                _waitEvent.Set();

                await Task.Delay(1000);

                _outputs[0] = false;
                _waitEvent.Set();
                return true;
            });

        }
        public Task<bool> OnGreenLight()
        {
            return Task.Run(async () =>
            {
                _outputs[2] = true;
                _waitEvent.Set();
                await Task.Delay(100);
                return true;
            });
        }
        public Task<bool> OffGreenLight()
        {
            return Task.Run(async () =>
            {
                _outputs[2] = false;
                _waitEvent.Set();
                await Task.Delay(100);
                return true;
            });
        }

        public Task<bool> OnRedLight()
        {
            return Task.FromResult(true);
            //return Task.Run(async () =>
            //{
            //    _outputs[(int)DO.RedLight] = true;
            //    _waitEvent.Set();
            //    await Task.Delay(100);
            //    return true;
            //});
        }
        public Task<bool> OffRedLight()
        {
            return Task.FromResult(true);
            //return Task.Run(async () =>
            //{
            //    _outputs[(int)DO.RedLight] = false;
            //    _waitEvent.Set();
            //    await Task.Delay(100);
            //    return true;
            //});
        }

        public Task<bool> StartGreenSpot()
        {
            return Task.Run(async () =>
            {
                _outputs[2] = true;
                _waitEvent.Set();

                await Task.Delay(1000);

                _outputs[2] = false;
                _waitEvent.Set();
                return true;
            });
        }
        public Task<bool> StartRedBlink()
        {
            return Task.Run(async () =>
            {
                _outputs[3] = true;
                _outputs[0] = true;
                _waitEvent.Set();

                await Task.Delay(500);

                _outputs[0] = false;
                _waitEvent.Set();
                await Task.Delay(500);
                _outputs[3] = false;
                _waitEvent.Set();
                return true;
            });
        }


        private static bool[] UshortToBoolArray(ushort value)
        {
            bool[] result = new bool[16];
            for (int i = 0; i < 16; i++)
            {
                result[i] = (value & (1 << i)) != 0;
            }
            return result;
        }

        private static ushort[] BoolArratyToUshortArray(bool[] array)
        {
            ushort[] value = { 0 };
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i])
                {
                    value[0] |= (ushort)(1 << i);
                }
            }
            return value;
        }
    }
}
