using BoxAgr.BLL.Events;
using BoxAgr.BLL.Interfaces;
using EasyModbus;
using Peripherals;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BoxAgr.BLL.Controllers
{
    public class ModBusController: IDisposable, IBusControl
    {

        private readonly ModbusServer easyModbusTCPServer;
        private readonly IModBusState _modBusState;


        private int _numberOfConnections;
      
        public event BoxInPositionHandler? BoxInPosition;
        public event PowerLossHandler? PowerLoss;
        public event SessionStateEvent? StatusChange; // событие изменение статуса.

        public bool IsBoxInPosition { get { return easyModbusTCPServer.coils[2]; } }
        public bool IsPowerLoss { get { return easyModbusTCPServer.coils[1]; } }

        public bool IsGreenLightActive => throw new NotImplementedException();

        public ModBusController( IModBusState modBusState ) 
        {
            _modBusState = modBusState;
           

            easyModbusTCPServer = new EasyModbus.ModbusServer();


            easyModbusTCPServer.CoilsChanged += new ModbusServer.CoilsChangedHandler(CoilsChanged);
            easyModbusTCPServer.HoldingRegistersChanged += new ModbusServer.HoldingRegistersChangedHandler(HoldingRegistersChanged);
            easyModbusTCPServer.NumberOfConnectedClientsChanged += new ModbusServer.NumberOfConnectedClientsChangedHandler(NumberOfConnectionsChanged);
            easyModbusTCPServer.LogDataChanged += new ModbusServer.LogDataChangedHandler(LogDataChanged);
           
        }

        public Task<bool> StartScan()
        {
            return Task.Run(async () => 
            {
                easyModbusTCPServer.discreteInputs[4] = true;
               
                await Task.Delay(1000);
                easyModbusTCPServer.discreteInputs[4] = false;
                return true;
            });
           
        }
        public Task<bool> StartGreenSpot()
        {
            return Task.Run(async () =>
            {
                easyModbusTCPServer.discreteInputs[3] = true;

                await Task.Delay(1000);
                easyModbusTCPServer.discreteInputs[3] = false;
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

        public Task<bool> StartRedBlink()
        {
            return Task.Run(async () =>
            {
                easyModbusTCPServer.discreteInputs[6] = true;
                //звук
                easyModbusTCPServer.discreteInputs[1] = true;
                await Task.Delay(500);
                easyModbusTCPServer.discreteInputs[1] = false;

                //await Task.Delay(1000);
                await Task.Delay(500);
                easyModbusTCPServer.discreteInputs[6] = false;
                return true;
            });
        }
        public Task<bool> StartSound()
        {
            return Task.Run(async () =>
            {
                easyModbusTCPServer.discreteInputs[6] = true;

                await Task.Delay(1000);
                easyModbusTCPServer.discreteInputs[6] = false;
                return true;
            });
        }
        delegate void coilsChangedCallback(int coil, int numberOfCoil);

        private bool boxInPos;
        private bool powerLoss;
        private void CoilsChanged(int coil, int numberOfCoil)
        {
            try
            {
                //_modBusState.Diagnostic = $"Coil1 = {easyModbusTCPServer.coils[1]} Coil2 = {easyModbusTCPServer.coils[2]} Coil3 = {easyModbusTCPServer.coils[3]}";

                if (easyModbusTCPServer.coils[1] != powerLoss)
                {
                    _modBusState.Power = easyModbusTCPServer.coils[1];
                    PowerLoss?.Invoke(this, easyModbusTCPServer.coils[1]);
                   

                }
                powerLoss = easyModbusTCPServer.coils[1];

                if (easyModbusTCPServer.coils[2] != boxInPos)
                {
                    _modBusState.IsBoxSet = easyModbusTCPServer.coils[2];
                    _modBusState.Diagnostic = _modBusState.IsBoxSet ? "Короб установлен" : "Короб не установлен";

                    
                    BoxInPosition?.Invoke(this, _modBusState.IsBoxSet);
                }
                boxInPos = easyModbusTCPServer.coils[2];

              
                

            }
            catch (Exception ex)
            {

            }
        }

        
        bool registersChanegesLocked;
        private void HoldingRegistersChanged(int register, int numberOfRegisters)
        {
            
        }


      
        private void NumberOfConnectionsChanged()
        {
            if (_numberOfConnections == easyModbusTCPServer.NumberOfConnections)
                return;

            _numberOfConnections = easyModbusTCPServer.NumberOfConnections;

            if (easyModbusTCPServer.NumberOfConnections > 0)
               StatusChange?.Invoke(2, PeripheralsType.Scanner, SessionStates.OnLine);
            else
               StatusChange?.Invoke(2, PeripheralsType.Scanner, SessionStates.Disconnect);
        }
        
       
        bool locked;
        private void LogDataChanged()
        {


            //try
            //{
            //    listBox1.Items.Clear();
            //    string listBoxData;
            //    for (int i = 0; i < easyModbusTCPServer.ModbusLogData.Length; i++)
            //    {
            //        if (easyModbusTCPServer.ModbusLogData[i] == null)
            //            break;
            //        if (easyModbusTCPServer.ModbusLogData[i].request)
            //        {
            //            listBoxData = easyModbusTCPServer.ModbusLogData[i].timeStamp.ToString("H:mm:ss.ff") + " Request from Client - Functioncode: " + easyModbusTCPServer.ModbusLogData[i].functionCode.ToString();
            //            if (easyModbusTCPServer.ModbusLogData[i].functionCode <= 4)
            //            {
            //                listBoxData = listBoxData + " ; Starting Address: " + easyModbusTCPServer.ModbusLogData[i].startingAdress.ToString() + " Quantity: " + easyModbusTCPServer.ModbusLogData[i].quantity.ToString();
            //            }
            //            if (easyModbusTCPServer.ModbusLogData[i].functionCode == 5)
            //            {
            //                listBoxData = listBoxData + " ; Output Address: " + easyModbusTCPServer.ModbusLogData[i].startingAdress.ToString() + " Output Value: ";
            //                if (easyModbusTCPServer.ModbusLogData[i].receiveCoilValues[0] == 0)
            //                    listBoxData = listBoxData + "False";
            //                if (easyModbusTCPServer.ModbusLogData[i].receiveCoilValues[0] == 0xFF00)
            //                    listBoxData = listBoxData + "True";
            //            }
            //            if (easyModbusTCPServer.ModbusLogData[i].functionCode == 6)
            //            {
            //                listBoxData = listBoxData + " ; Starting Address: " + easyModbusTCPServer.ModbusLogData[i].startingAdress.ToString() + " Register Value: " + easyModbusTCPServer.ModbusLogData[i].receiveRegisterValues[0].ToString();
            //            }
            //            if (easyModbusTCPServer.ModbusLogData[i].functionCode == 15)
            //            {
            //                listBoxData = listBoxData + " ; Starting Address: " + easyModbusTCPServer.ModbusLogData[i].startingAdress.ToString() + " Quantity: " + easyModbusTCPServer.ModbusLogData[i].quantity.ToString() + " Byte Count: " + easyModbusTCPServer.ModbusLogData[i].byteCount.ToString() + " Values Received: ";
            //                for (int j = 0; j < easyModbusTCPServer.ModbusLogData[i].quantity; j++)
            //                {
            //                    int shift = j % 16;
            //                    if ((i == easyModbusTCPServer.ModbusLogData[i].quantity - 1) & (easyModbusTCPServer.ModbusLogData[i].quantity % 2 != 0))
            //                    {
            //                        if (shift < 8)
            //                            shift = shift + 8;
            //                        else
            //                            shift = shift - 8;
            //                    }
            //                    int mask = 0x1;
            //                    mask = mask << (shift);
            //                    if ((easyModbusTCPServer.ModbusLogData[i].receiveCoilValues[j / 16] & (ushort)mask) == 0)
            //                        listBoxData = listBoxData + " False";
            //                    else
            //                        listBoxData = listBoxData + " True";
            //                }
            //            }
            //            if (easyModbusTCPServer.ModbusLogData[i].functionCode == 16)
            //            {
            //                listBoxData = listBoxData + " ; Starting Address: " + easyModbusTCPServer.ModbusLogData[i].startingAdress.ToString() + " Quantity: " + easyModbusTCPServer.ModbusLogData[i].quantity.ToString() + " Byte Count: " + easyModbusTCPServer.ModbusLogData[i].byteCount.ToString() + " Values Received: ";
            //                for (int j = 0; j < easyModbusTCPServer.ModbusLogData[i].quantity; j++)
            //                {
            //                    listBoxData = listBoxData + " " + easyModbusTCPServer.ModbusLogData[i].receiveRegisterValues[j];
            //                }
            //            }
            //            if (easyModbusTCPServer.ModbusLogData[i].functionCode == 23)
            //            {
            //                listBoxData = listBoxData + " ; Starting Address Read: " + easyModbusTCPServer.ModbusLogData[i].startingAddressRead.ToString() + " ; Quantity Read: " + easyModbusTCPServer.ModbusLogData[i].quantityRead.ToString() + " ; Starting Address Write: " + easyModbusTCPServer.ModbusLogData[i].startingAddressWrite.ToString() + " ; Quantity Write: " + easyModbusTCPServer.ModbusLogData[i].quantityWrite.ToString() + " ; Byte Count: " + easyModbusTCPServer.ModbusLogData[i].byteCount.ToString() + " ; Values Received: ";
            //                for (int j = 0; j < easyModbusTCPServer.ModbusLogData[i].quantityWrite; j++)
            //                {
            //                    listBoxData = listBoxData + " " + easyModbusTCPServer.ModbusLogData[i].receiveRegisterValues[j];
            //                }
            //            }

            //            listBox1.Items.Add(listBoxData);
            //        }
            //        if (easyModbusTCPServer.ModbusLogData[i].response)
            //        {
            //            if (easyModbusTCPServer.ModbusLogData[i].exceptionCode > 0)
            //            {
            //                listBoxData = easyModbusTCPServer.ModbusLogData[i].timeStamp.ToString("H:mm:ss.ff");
            //                listBoxData = listBoxData + (" Response To Client - Error code: " + Convert.ToString(easyModbusTCPServer.ModbusLogData[i].errorCode, 16));
            //                listBoxData = listBoxData + " Exception Code: " + easyModbusTCPServer.ModbusLogData[i].exceptionCode.ToString();
            //                listBox1.Items.Add(listBoxData);


            //            }
            //            else
            //            {
            //                listBoxData = (easyModbusTCPServer.ModbusLogData[i].timeStamp.ToString("H:mm:ss.ff") + " Response To Client - Functioncode: " + easyModbusTCPServer.ModbusLogData[i].functionCode.ToString());

            //                if (easyModbusTCPServer.ModbusLogData[i].functionCode <= 4)
            //                {
            //                    listBoxData = listBoxData + " ; Bytecount: " + easyModbusTCPServer.ModbusLogData[i].byteCount.ToString() + " ; Send values: ";
            //                }
            //                if (easyModbusTCPServer.ModbusLogData[i].functionCode == 5)
            //                {
            //                    listBoxData = listBoxData + " ; Starting Address: " + easyModbusTCPServer.ModbusLogData[i].startingAdress.ToString() + " ; Output Value: ";
            //                    if (easyModbusTCPServer.ModbusLogData[i].receiveCoilValues[0] == 0)
            //                        listBoxData = listBoxData + "False";
            //                    if (easyModbusTCPServer.ModbusLogData[i].receiveCoilValues[0] == 0xFF00)
            //                        listBoxData = listBoxData + "True";
            //                }
            //                if (easyModbusTCPServer.ModbusLogData[i].functionCode == 6)
            //                {
            //                    listBoxData = listBoxData + " ; Starting Address: " + easyModbusTCPServer.ModbusLogData[i].startingAdress.ToString() + " ; Register Value: " + easyModbusTCPServer.ModbusLogData[i].receiveRegisterValues[0].ToString();
            //                }
            //                if (easyModbusTCPServer.ModbusLogData[i].functionCode == 15)
            //                {
            //                    listBoxData = listBoxData + " ; Starting Address: " + easyModbusTCPServer.ModbusLogData[i].startingAdress.ToString() + " ; Quantity: " + easyModbusTCPServer.ModbusLogData[i].quantity.ToString();
            //                }
            //                if (easyModbusTCPServer.ModbusLogData[i].functionCode == 16)
            //                {
            //                    listBoxData = listBoxData + " ; Starting Address: " + easyModbusTCPServer.ModbusLogData[i].startingAdress.ToString() + " ; Quantity: " + easyModbusTCPServer.ModbusLogData[i].quantity.ToString();
            //                }
            //                if (easyModbusTCPServer.ModbusLogData[i].functionCode == 23)
            //                {
            //                    listBoxData = listBoxData + " ; ByteCount: " + easyModbusTCPServer.ModbusLogData[i].byteCount.ToString() + " ; Send Register Values: ";
            //                }
            //                if (easyModbusTCPServer.ModbusLogData[i].sendCoilValues != null)
            //                {
            //                    for (int j = 0; j < easyModbusTCPServer.ModbusLogData[i].sendCoilValues.Length; j++)
            //                    {
            //                        listBoxData = listBoxData + easyModbusTCPServer.ModbusLogData[i].sendCoilValues[j].ToString() + " ";
            //                    }
            //                }
            //                if (easyModbusTCPServer.ModbusLogData[i].sendRegisterValues != null)
            //                {
            //                    for (int j = 0; j < easyModbusTCPServer.ModbusLogData[i].sendRegisterValues.Length; j++)
            //                    {
            //                        listBoxData = listBoxData + easyModbusTCPServer.ModbusLogData[i].sendRegisterValues[j].ToString() + " ";
            //                    }
            //                }
            //                listBox1.Items.Add(listBoxData);
            //            }
            //        }
            //    }
            //}
            //catch (Exception) { }

            locked = false;




        }

        public void Start(CancellationToken cancelationToken)
        {
            easyModbusTCPServer.Listen();
        }

        public void Dispose()
        {
            
               
        }

        public Task<bool> OnGreenLight()
        {
            throw new NotImplementedException();
        }

        public Task<bool> OffGreenLight()
        {
            throw new NotImplementedException();
        }
    }
}
