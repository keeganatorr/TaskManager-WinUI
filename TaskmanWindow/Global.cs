using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskmanWindow
{
	public static class Global
	{
		public static ObservableCollection<OpenProcess> ProcessList = new ObservableCollection<OpenProcess>();

	}
}
