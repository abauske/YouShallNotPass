using System;

public class RequiredDistDyn : RequiredDistIter
{
    private double ml;
    private double m = 2300;
    private double vp = 50 / 3.6;
    private double P = 1.82691356e+05;
    private double n = 1;
    private double u = 1.49545869e+00;
    private double A = 2.53731408e+00;
    private double cw = 3.80564949e-01;
    private double pair = 1.2041;
    private double Cr = 5.98281045e-12;
    public double vwind = 0;
    
    public RequiredDistDyn(double dt, bool awd = false) : base(dt)
    {
        ml = m / (awd ? 1 : 2);
    }

    double beta(double v)
    {
        return 9.05166480e-01;// (1 + Math.Min(v, vp) * (1 - 1 / vp)) / vp;
    }
    double Fa(double v)
    {
        return Math.Min(n * beta(v) * P / v, ml * g * u);
    }
    
    double Fr(double v, double G)
    {
        double dv = v + vwind;
        return 0.5 * A * cw * pair * dv * dv + g * m * (1 - G) * Cr + g * m * G;
    }

    protected override double a(double v, double G, double vwind)
    {
        return (Fa(v) - Fr(v, G)) / m;
    }
}
