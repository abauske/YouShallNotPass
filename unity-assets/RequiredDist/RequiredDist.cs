
public abstract class RequiredDist
{
    public const double g = 9.81;
    public const double abrake = 3.3;
    public const double lambda = 0.8;
    
    /**
     * startupTime: time until oing cars front is at same height as oen cars back
     * returns (requiredDist, tfinish, voingend, startupTime, startupDist)
     */
    public abstract (double, double, double, double, double) getRequiredDist(double voing, double LtoCrit, double Lrest, double voen, double G, double vwind, double vlimkmh, double Leges);
}
