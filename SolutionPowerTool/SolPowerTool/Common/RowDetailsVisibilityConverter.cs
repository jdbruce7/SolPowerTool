using System;
using System.Globalization;
using System.Windows.Data;
using Microsoft.Windows.Controls;

namespace SolPowerTool.App.Common
{
    public class RowDetailsVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (DataGridRowDetailsVisibilityMode) value;
            var param = (string) parameter;
            switch (val)
            {
                case DataGridRowDetailsVisibilityMode.Collapsed:
                    return param == "Never";
                case DataGridRowDetailsVisibilityMode.VisibleWhenSelected:
                    return param == "Selected";
                case DataGridRowDetailsVisibilityMode.Visible:
                    return param == "Always";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (bool) value;
            var param = (string) parameter;
            switch (param)
            {
                case "Never":
                    return DataGridRowDetailsVisibilityMode.Collapsed;
                case "Selected":
                    return DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
                case "Always":
                    return DataGridRowDetailsVisibilityMode.Visible;
                default:
                    return null;
            }
        }

        #endregion
    }
}