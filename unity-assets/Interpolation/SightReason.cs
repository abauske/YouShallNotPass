using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SightLimitReason
{
	Bump,
	Corner,
	EndOfRoad,
	SameAsStart
}

[Serializable]
public class SightReason
{

	// Save the original x and y for Eval
	[SerializeField] private float[] xOrig;
	public float[] XOrig => xOrig;
	[SerializeField] private SightLimitReason[] yOrig;
	public SightLimitReason[] YOrig => yOrig;

	public SightReason(float[] x, SightLimitReason[] y)
	{
		xOrig = x;
		yOrig = y;
	}

	private int _lastIndex = 0;

	/// <summary>
	/// Find where in xOrig the specified x falls, by simultaneous traverse.
	/// This allows xs to be less than x[0] and/or greater than x[n-1]. So allows extrapolation.
	/// This keeps state, so requires that x be sorted and xs called in ascending order, and is not multi-thread safe.
	/// </summary>
	private int GetNextXIndex(float x)
	{
		if (x < xOrig[_lastIndex])
		{
			throw new ArgumentException("The X values to evaluate must be sorted.");
		}

		while ((_lastIndex < xOrig.Length - 2) && (x > xOrig[_lastIndex + 1]))
		{
			_lastIndex++;
		}

		return _lastIndex;
	}

	public SightLimitReason Eval(float x, bool debug = false, bool resetSearch = true)
	{
		if (resetSearch)
		{
			_lastIndex = 0; // Reset simultaneous traversal in case there are multiple calls
		}

		// Evaluate using j'th spline
		return yOrig[Math.Min(yOrig.Length - 1, Math.Max(0, GetNextXIndex(x)))];
	}
}
