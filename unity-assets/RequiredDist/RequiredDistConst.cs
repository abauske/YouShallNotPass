using System;
using UnityEngine;

public class RequiredDistConst : RequiredDist
{
    private double a = 2.99999502 * lambda;

    public override (double, double, double, double, double) getRequiredDist(double voing, double LtoCrit, double Lrest, double voen, double G, double vwind, double vlimkmh, double Leges)
    {
        double aeff = a - g * G;
        double Lges = LtoCrit + Lrest;
        double vmaxoing = 1.05 / 3.6 * vlimkmh;
        double tvmax = Math.Max(0, (vmaxoing - voing) / aeff);
        double dv = voing - voen;
        
        // Calculate time for oing cars front to be at same height as oen cars back
        double teqa = (voen - voing + Math.Sqrt(2 * aeff * LtoCrit + dv * dv)) / aeff;
        double teqvmax = (LtoCrit + (voen - voing) * tvmax - 1 / 2f * aeff * tvmax * tvmax) / (vmaxoing - voen) + tvmax;
        double teq = teqa <= tvmax ? teqa : teqvmax;
        double seq = LtoCrit + teq * voen;
        
        // Calculate without brake required
        double hatt = (voen - voing + Math.Sqrt(2 * aeff * Lges + dv * dv)) / aeff;
        double tgesvmax = (Lges + (voen - voing) * tvmax - 1 / 2f * aeff * tvmax * tvmax) / (vmaxoing - voen) + tvmax;
        double tnobrake = hatt <= tvmax ? hatt : tgesvmax;
        double snobrake = Lges + voen * tnobrake;
        double vmaxnobrake = Math.Min(vmaxoing, tnobrake * aeff + voing);
        if (Leges >= Single.PositiveInfinity)
        {
            Debug.Log("tges: " + tnobrake + "    lin a: " + (2 * (snobrake - voing * tnobrake) / tnobrake / tnobrake));
        
            return (snobrake - seq, tnobrake - teq, vmaxnobrake, teq, seq);
        }
        
        // Calculate with brake required
        
        // Vmax reached
        double hattacc = tvmax;
        double hattbrake = (vmaxoing - voen) / abrake;
        double hatdbrake = -1 / 2f * abrake * hattbrake * hattbrake + vmaxoing * hattbrake;
        double hatdacc = 1 / 2f * aeff * hattacc * hattacc + voing * hattacc;

        if (hatdacc + hatdbrake < Lges + (hattacc + hattbrake) * voen)
        {
            double hattcv = (Leges + (hattacc + hattbrake) * voen - hatdacc - hatdbrake) / (vmaxoing - voen);
            double hatdcv = vmaxoing * hattcv;

            if (hatdacc >= Lges + voen * hattacc)
            {
                // We can return to our lane during acceleration -> pure accel maneuver
                return (snobrake - seq, tnobrake - teq, vmaxnobrake, teq, seq);
            }

            if (hatdacc + hatdcv >= Lges + voen * (hattacc + hattcv))
            {
                // we can return to our lane during constant velocity -> accel with max lim reached -> as above
                return (snobrake - seq, tnobrake - teq, vmaxnobrake, teq, seq);
            }
            
            double voenminusmax = voen - vmaxoing; 
            
            // Calculate teq:
            if (hatdacc + hatdcv < LtoCrit)
            {
                // we need to update teq as the other car is reached during braking. This is probably very rare but still possible theoretically!
                teq = hattacc + hattcv + (vmaxoing - voen - Math.Sqrt(voenminusmax * voenminusmax + 2 * abrake * (hatdacc + hatdcv - LtoCrit - voen * (hattacc + hattcv)))) / abrake;
                seq = hatdacc + hatdcv + teq * vmaxoing - 1 / 2f * abrake * teq * teq;
            }

            double hattbrakeslash = (vmaxoing - voen - Math.Sqrt(voenminusmax * voenminusmax + 2 * abrake * (hatdacc + hatdcv - Lges - voen * (hattacc + hattcv)))) / abrake;
            double hatendspeed = vmaxoing - hattbrakeslash * abrake;
            double hattgesbrake = hattacc + hattcv + hattbrakeslash;
            return (hatdacc + hatdcv + hattbrakeslash * vmaxoing - 1 / 2f * abrake * hattbrakeslash * hattbrakeslash - seq, hattgesbrake - teq, hatendspeed, teq, seq);
        }

        // Vmax not reached
        double voingoen = voing - voen;
        double checktacc =
            (Math.Sqrt(((aeff + abrake) * (2 * aeff * Leges + voingoen * voingoen)) / abrake) +
             ((aeff + abrake) * (voen - voing)) / abrake) / (aeff * aeff / abrake + aeff);
        double reachedSpeed = aeff * checktacc + voing;
        double tempV = reachedSpeed - voen;
        double checkdacc = 1 / 2f * aeff * checktacc * checktacc + voing * checktacc;

        if (checkdacc >= Lges + voen * checktacc)
        {
            // We can return to our lane during acceleration -> pure accel maneuver
            return (snobrake - seq, tnobrake - teq, vmaxnobrake, teq, seq);
        }

        // We return to our lane during braking
        
        // Calculate teq:
        if (checkdacc < LtoCrit)
        {
            // we need to update teq as the other car is reached during braking. This is probably very rare but still possible theoretically!
            teq = checktacc - Math.Sqrt(tempV * tempV - 2 * abrake * (LtoCrit - checkdacc + checktacc * voen)) / abrake +
                  tempV / abrake;
            seq = checkdacc + teq * reachedSpeed - 1 / 2f * abrake * teq * teq;
        }
        
        double checktbrake =
            -Math.Sqrt(tempV * tempV - 2 * abrake * (Lges - checkdacc + checktacc * voen)) / abrake +
            tempV / abrake;
        double checkdbrake = (voing + aeff * checktacc) * checktbrake - 1 / 2f * abrake * checktbrake * checktbrake;
        double checktges = checktbrake + checktacc;
        double checkendspeed = voing + aeff * checktacc - abrake * checktbrake;
        return (checkdacc + checkdbrake - seq, checktges - teq, checkendspeed, teq, seq);
    }
}
