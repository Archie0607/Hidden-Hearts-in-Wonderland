using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hidden_Hearts_in_Wonderland.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            // แจ้งหน้า UI ว่าค่าที่ bind อยู่เปลี่ยนแล้ว ให้รีเฟรชบนจอ
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
