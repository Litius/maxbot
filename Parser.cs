using System.Globalization;

namespace maxbot
{
    /// <summary>
    /// Парсек строки, введённой пользователем
    /// </summary>
    public static class QuadraticParser
    {
        public static bool TryParse(string? input, out double a, out double b, out double c)
        {
            //инициализация переменных
            a = b = c = 0;

            //если строка отсутсвует, пустая или стостоит только из пробелов
            if (string.IsNullOrWhiteSpace(input)) 
                return false;

            //разделяем строку на части, разделитель пробел, пустые части удаляем
            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            //если после разделения число частей (т.к. пользователь ввёл НЕ 3 числа) не соответствует ожидаемому
            if (parts.Length != 3)
                return false;

            //преобразуем полученные строки в вещественные числа
            return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out a)
                   && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out b)
                   && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out c);
        }
    }
}
