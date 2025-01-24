using System;

public abstract class RequiredDistIter : RequiredDist
{
    protected const double MAX_TIME = 40;
    
    protected double dt { get; }
    
    public RequiredDistIter(double dt)
    {
        this.dt = dt;
    }

    protected abstract double a(double v, double G, double vwind);

    double v(double t, double G, double vwind)
    {
        if (t < dt)
        {
            return 0;
        }

        double speed = v(t - dt, G, vwind);
        return speed + a(speed, G, vwind) * dt;
    }

    double s(double time, double G, double vwind)
    {
        double a = 0;
        double v = 0;
        double s = 0;
        for (double t = 0; t <= time; t += dt)
        {
            a = this.a(v, G, vwind);
            v += a * dt;
            s += v * dt;
        }
        return s;
    }
    
    public override (double, double, double, double, double) getRequiredDist(double voing, double LtoCrit, double Lrest, double voen, double G, double vwind, double vlimkmh, double Leges)
    {
        double Lges = LtoCrit + Lrest;
        double vmaxoing = 1.05 / 3.6 * vlimkmh;
        
        double v = voing;
        double s = 0;
        double startupTime = -1;
        for (double t = dt; t <= MAX_TIME; t += dt)
        {
            double a = this.a(v, G, vwind) * lambda;
            v = Math.Min(v + a * dt, vmaxoing);
            s += v * dt;

            double tbrake = (v - voen) / abrake;
            double dbrake = -1 / 2f * abrake * tbrake * tbrake + v * tbrake;

            var doen = voen * t;

            if (s >= LtoCrit + doen && startupTime < 0)
            {
                startupTime = t;
            }
            
            double dmin = Lges + doen;
            // Console.WriteLine("t: " + t + "     a: " + a + "        v: " + v + "          s: " + s + "       dmin: " + dmin + " inf " + Double.PositiveInfinity);
            if (s >= dmin)
            {
                Console.WriteLine("tges: " + t + "    lin a: " + (2 * (s - voing * t) / t / t));
                double startupDist = voen * startupTime + LtoCrit;
                return (dmin - startupDist, t - startupTime, v, startupTime, startupDist);
            }

            double dmax = Leges + voen * (t + tbrake);
            if (dmax <= s + dbrake)
            {
                double dv = v - voen;
                double tbrakeslash = (dv - Math.Sqrt(dv * dv + 2 * abrake * (s - Lges - doen))) / abrake;
                double dbrakeslash = -1 / 2f * abrake * tbrakeslash * tbrakeslash + v * tbrakeslash;
                double endspeed = v - tbrakeslash * abrake;
                double startupDist = voen * startupTime + LtoCrit;
                return (s + dbrakeslash - startupDist, t + tbrakeslash - startupTime, endspeed, startupTime, startupDist);
            }
        }

        return (Double.MaxValue, Double.MaxValue, Double.MaxValue, Double.MaxValue, Double.MaxValue);
    }
}
