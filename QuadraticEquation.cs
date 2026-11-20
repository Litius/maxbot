using System.Numerics;


/// <summary>
/// Возможные результаты решения уравнения
/// </summary>
public enum RootsKind
{
    None,        // корней нет
    One,         // один корень (дискриминант = 0)
    Two         // два различных корня
}

/// <summary>
/// Результат вычисления
/// </summary>
/// <param name="Kind"></param>
/// <param name="X1"></param>
/// <param name="X2"></param>
public readonly record struct QuadraticResult(
    RootsKind Kind,
    double? X1,
    double? X2);

public class QuadraticEquation(double a, double b, double c)
{
    public double A { get; } = a;
    public double B { get; } = b;
    public double C { get; } = c;

    public double Discriminant => B * B - 4 * A * C;

    public QuadraticResult Solve()
    {       

        double d = Discriminant;

        if (d > 0)
        {
            double sqrtD = Math.Sqrt(d);
            double x1 = (-B + sqrtD) / (2 * A);
            double x2 = (-B - sqrtD) / (2 * A);

            return new QuadraticResult(RootsKind.Two, x1, x2);
        }

        if (d == 0)
        {
            double x = -B / (2 * A);
            return new QuadraticResult(RootsKind.One, x, null);
        }

        // d < 0
        return new QuadraticResult(
            RootsKind.None, null, null);
    }

    public override string ToString() => $"{A}x² + {B}x + {C} = 0";
}