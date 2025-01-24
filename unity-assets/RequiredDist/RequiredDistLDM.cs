
public class RequiredDistLDM : RequiredDistIter
{
    private double amax = 7.79507133;
    private double ve = 41.84745951;
    
    public RequiredDistLDM(double dt) : base(dt)
    {
    }

    protected override double a(double v, double G, double vwind)
    {
        return amax - amax / ve * v - g * G;
    }
}
