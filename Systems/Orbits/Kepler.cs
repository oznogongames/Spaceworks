﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

namespace Spaceworks.Orbits.Kepler {

    /// <summary>
    /// Class for utlities for kepler calculations
    /// </summary>
    public class KeplerUtils {

        /// <summary>
        /// Inverse Hyperbolic Cosine
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static double Acosh(double a) {
            if (a < 1)
                return 0;
            return Math.Log(a + Math.Sqrt(a * a - 1.0d));
        }

        /// <summary>
        /// Convert eccentric anomaly to true anomaly
        /// https://en.wikipedia.org/wiki/True_anomaly
        /// </summary>
        /// <param name="eccentricAnomaly"></param>
        /// <param name="eccentricity"></param>
        /// <returns></returns>
        public static double EccentricToTrue(double eccentricAnomaly, double eccentricity) {
            if (eccentricity < 1) {
                double cosE = Math.Cos(eccentricAnomaly);

                double t = Math.Acos(
                    (cosE - eccentricity) /
                    (1 - eccentricity * cosE)
                );

                return t;
            }
            else {
                return Math.Atan2(
                    Math.Sqrt(eccentricity * eccentricity - 1) * Math.Sinh (eccentricAnomaly),
                    eccentricity - Math.Cosh (eccentricAnomaly)
                );
            }
        }

        /// <summary>
        /// Convert true anomaly to eccentric anomaly
        /// </summary>
        /// <param name="trueAnomaly"></param>
        /// <param name="eccentricity"></param>
        /// <returns></returns>
        public static double TrueToEccentric(double trueAnomaly, double eccentricity) {
            if (double.IsNaN(eccentricity) || double.IsInfinity(eccentricity)) {
                return trueAnomaly;
            }

            trueAnomaly = trueAnomaly % KeplerConstants.TWO_PI;

            if (eccentricity < 1) {
                if (trueAnomaly < 0) {
                    trueAnomaly += KeplerConstants.TWO_PI;
                }

                //Inverse of the Eccentric to True
                double cosT = Math.Cos(trueAnomaly);
                double ecc = Math.Acos(
                    (eccentricity + cosT) /
                    (1 + eccentricity * cosT)
                );

                if (trueAnomaly > KeplerConstants.PI) {
                    ecc = KeplerConstants.TWO_PI - ecc;
                }

                return ecc;
            }
            else {
                double cosT = Math.Cos(trueAnomaly);
                double ecc = Acosh(
                    ((eccentricity + cosT) /
                    (1 + eccentricity * cosT)) * Math.Sign(trueAnomaly)
                );
                return ecc;
            }
        }

        /// <summary>
        /// Convert the mean anomaly to the eccentric anomaly
        /// </summary>
        /// <param name="meanAnomaly"></param>
        /// <param name="eccentricity"></param>
        /// <returns></returns>
        public static double MeanToEccentric(double meanAnomaly, double eccentricity) {
            if (eccentricity < 1) {
                return SolveKeplerEquation(meanAnomaly, eccentricity);
            }
            else {
                return SolveHyperbolicKeplerEquation(meanAnomaly, eccentricity);    
            }
        }

        /// <summary>
        /// Convert the eccentric anomaly to the mean anomaly
        /// </summary>
        /// <param name="eccentricAnomaly"></param>
        /// <param name="eccentricity"></param>
        /// <returns></returns>
        public static double EccentricToMean(double eccentricAnomaly, double eccentricity) {
            if (eccentricity < 1) {
                return eccentricAnomaly - eccentricity * Math.Sin(eccentricAnomaly);
            }
            else {
                return Math.Sinh(eccentricAnomaly) * eccentricity - eccentricAnomaly;
            }
        }

        /// <summary>
        /// Iterative approach to solve the kepler equation
        /// </summary>
        /// <param name="meanAnomaly"></param>
        /// <param name="eccentricity"></param>
        /// <returns></returns>
        private static double SolveKeplerEquation(double meanAnomaly, double eccentricity) {
            //Iterative approach
            int iterations = eccentricity < 0.4 ? 2 : 4;
            double e = meanAnomaly;

            for (int i = 0; i < iterations; i++) {
                double esin = eccentricity * Math.Sin(e);
                double ecos = eccentricity * Math.Cos(e);
                double del = e - esin - meanAnomaly;
                double n = 1 - ecos;

                e += -5 * del / (n + Math.Sign(n) * Math.Sqrt(Math.Abs(16 * n * n - 20 * del * esin)));
            }

            return e;
        }

        /// <summary>
        /// Iterative approach to solve the kepler equation for hyperbolic orbits
        /// </summary>
        /// <param name="meanAnomaly"></param>
        /// <param name="eccentricity"></param>
        /// <returns></returns>
        public static double SolveHyperbolicKeplerEquation(double meanAnomaly, double eccentricity) {
            double ep = 1e-005d;
            double del = 1;
            
            double f = Math.Log(2d * System.Math.Abs(meanAnomaly) / eccentricity + 1.8d);
            if (double.IsNaN(f) || double.IsInfinity(f)) {
                return meanAnomaly;
            }

            int maxIteration = 1000; int i = 0;
            while (Math.Abs(del) > ep) {
                del = (eccentricity * (float)System.Math.Sinh(f) - f - meanAnomaly) / (eccentricity * (float)System.Math.Cosh(f) - 1d);
                if (double.IsNaN(del) || double.IsInfinity(del)) {
                    return f;
                }
                
                f -= del;

                if (!(i < ++maxIteration))
                    break;
            }

            return f;
        }

    }

    /// <summary>
    /// Class for storing required parameters to create keplerian orbits
    /// </summary>
    [System.Serializable]
    public class KeplerOrbitalParameters {
        /// <summary>
        /// Length of the semi-major axis 'a'
        /// </summary>
        public double semiMajorLength;

        /// <summary>
        /// Eccentricity describing the shape of the ellipse 'e'
        /// </summary>
        public double eccentricity;

        /// <summary>
        /// Mean anomaly angle corresponding to the body's eccentric anomaly
        /// </summary>
        public double meanAnomaly;

        /// <summary>
        /// Angle of inclination of orbital plane
        /// </summary>
        public double inclination;

        /// <summary>
        /// Angle of argument for the periapsis. Angle from ascending node to periapsis
        /// </summary>
        public double perifocus;

        /// <summary>
        /// Angle of reference to where the orbit passes through the plane of reference 
        /// </summary>
        public double ascendingNode;

        /// <summary>
        /// Create orbital parameters from an object in a cartesian coordinate system centered on a large mass in which to orbit
        /// https://en.wikipedia.org/wiki/Orbital_elements
        /// http://orbitsimulator.com/formulas/OrbitalElements.html
        /// </summary>
        /// <param name="mu">standard gravitational parameter</param>
        /// <param name="position">relative position</param>
        /// <param name="velocity">velocity</param>
        /// <returns></returns>
        public static KeplerOrbitalParameters SolveOrbitalParameters(double mu, Vector3d position, Vector3d velocity) {
            KeplerOrbitalParameters kop = new KeplerOrbitalParameters();

            Vector3d r = position;
            Vector3d v = velocity;

            double R = r.magnitude;
            double V = v.magnitude;

            //Semi major axis
            double a = 1 / (2 / R - V * V / mu);

            //Angular momentum
            Vector3d h = Vector3d.Cross(r, v);
            double H = h.magnitude;

            Vector3d node = Vector3d.Cross(Vector3d.up, h);

            //...
            double p = H * H / mu;
            double q = Vector3d.Dot(r, v);

            //Eccentricity
            Vector3d e = (V * V / mu - 1 / R) * r - (q / mu) * v;
            double E = Math.Sqrt(1.0d - p / a);
            double Ex = 1.0d - R / a;
            double Ez = q / Math.Sqrt(a * mu);

            //Inclination (0 - 180 degrees)
            double i = Math.Acos(h.y / H);
            if (i >= KeplerConstants.PI) {
                i = KeplerConstants.PI - i;
            }
            if (i == KeplerConstants.TWO_PI)
                i = 0;
            double lan = 0;
            if (i % KeplerConstants.PI != 0) {
                lan = Math.Atan2(h.x, -h.z);
            }

            //Argument of perifocus
            double W;
            if (i == 0) {
                //Equitorial orbit
                W = Math.Atan2(e.z, e.x);
            }
            else {
                W = Math.Acos(
                    Vector3d.Dot(node, e) /
                    (node.magnitude * E)
                );
            }
            if (W < 0) {
                W = KeplerConstants.TWO_PI + W;
            }

            //Compute anomalies
            double tAnom = Math.Acos(Vector3d.Dot(e, r) / (E * R));
            if (q < 0)
                tAnom = KeplerConstants.TWO_PI - tAnom;
            double eAnom = KeplerUtils.TrueToEccentric(tAnom, E);
            double mAnom = KeplerUtils.EccentricToMean(eAnom, E);
            //double eAnom = Math.Atan2(Ez, Ex); //eccentric anomaly
            //double mAnom = eAnom - E * Math.Sin(eAnom); //mean anomaly

            //Set the values
            kop.ascendingNode = lan;
            kop.eccentricity = E;
            kop.inclination = i;
            kop.meanAnomaly = mAnom;
            kop.perifocus = W;
            kop.semiMajorLength = a;

            return kop;
        }

        public override string ToString() {
            return JsonUtility.ToJson(this, true);
        }
    }

    /// <summary>
    /// List of constants used in kepler orbit calculations
    /// </summary>
    public class KeplerConstants {
        //Misc

        public static double EPSILON = double.Epsilon;

        //Distance

        public static double KILOMETER_TO_METER = 1000;
        public static double AU_TO_METER = 1.496e+11d;
        public static double LIGHT_SECOND_TO_METER = 2.998e+8d;
        public static double LIGHT_MINUTE_TO_METER = 1.799e+10d;
        public static double LIGHT_HOUR_TO_METER = 1.079e+12d;
        public static double LIGHT_DAY_TO_METER = 2.59e+13d;
        public static double LIGHT_YEAR_TO_METER = 9.461e+15d;

        public static double EARTH_RADIUS = 6371 * KILOMETER_TO_METER;

        //Time

        public static int HOURS_TO_SECONDS = 3600;
        public static int DAYS_TO_SECONDS = 86400;
        public static int YEARS_TO_SECONDS = DAYS_TO_SECONDS * 365;

        //Rotation

        public static double DEGREE_TO_RADIAN = 0.0174533d;
        public static double PI = Math.PI;
        public static double TWO_PI = 2 * PI;

        //Mass

        public static int TONNE_TO_KG = 1000;
        public static double EARTH_MASS = 5.974e24d;
        public static double SOLAR_MASS = 1.9891e30d;
        public static double SHUTTLE_MASS = 2027557.894d;

        //Gravity

        public static double G = 6.674e-11d;
        public static double DEG_TO_RAD = Math.PI / (180);
        public static double RAD_TO_DEG = (180d) / Math.PI;
    }

    /// <summary>
    /// Class decribing keplerian orbit
    /// </summary>
    public class KeplerOrbit {

        #region base_parameters
        /// <summary>
        /// Body in which this orbit defined around
        /// </summary>
        public KeplerBody parent { get; private set; }

        /// <summary>
        /// Length of the semi-major axis 'a'
        /// </summary>
        public double semiMajorLength { get; private set; }

        /// <summary>
        /// Shortcut for semi-major axis length
        /// </summary>
        public double a {
            get {
                return semiMajorLength;
            }
        }

        /// <summary>
        /// Eccentricity describing the shape of the ellipse 'e'
        /// </summary>
        public double eccentricity { get; private set; }

        /// <summary>
        /// Mean anomaly angle corresponding to the body's eccentric anomaly
        /// </summary>
        public double meanAnomaly { get; private set; }

        /// <summary>
        /// Angle of inclination of orbital plane
        /// </summary>
        public double inclination { get; private set; }

        /// <summary>
        /// Angle of argument for the periapsis. Angle from ascending node to periapsis
        /// </summary>
        public double perifocus { get; private set; }

        /// <summary>
        /// Angle of reference to where the orbit passes through the plane of reference 
        /// </summary>
        public double ascendingNode { get; private set; }

        #endregion

        #region derivable_parameters
        /// <summary>
        /// Normal of the orbital plane in world XYZ coordinates
        /// </summary>
        public Vector3d normal {
            get {
                Vector3d planeNormal = Vector3d.up;

                //Rotate to match inclination
                planeNormal = Vector3d.Rotate(planeNormal, this.inclination, Vector3d.forward);

                //Rotate to align the ascending node to the reference direction
                planeNormal = Vector3d.Rotate(planeNormal, this.ascendingNode, Vector3d.up);

                return planeNormal;
            }
        }

        /// <summary>
        /// Length of the semi-minor axis
        /// </summary>
        public double semiMinorAxis {
            get {
                return a * Math.Sqrt(1 - eccentricity * eccentricity);
            }
        }

        /// <summary>
        /// Shortcut for the length of the semi-minor axis
        /// </summary>
        public double b {
            get {
                return semiMinorAxis;
            }
        }

        /// <summary>
        /// Product of G and the mass of the heavier object (m^3/s^2)
        /// </summary>
        public double standardGravitationalParameter {
            get {
                return this.parent.mass * KeplerConstants.G;
            }
        }

        /// <summary>
        /// Length to the periapsis (m)
        /// </summary>
        public double periapsis {
            get {
                //Elliptical
                if (eccentricity < 1) {
                    return (1 - eccentricity) * a;
                }
                //Parabolic
                else if (eccentricity == 1) {
                    return a;
                }
                //Hyperbolic
                else {
                    return (1 - eccentricity) * a;
                }
            }
        }

        /// <summary>
        /// Time to periapsis (s)
        /// </summary>
        public double periapsisTime {
            get {
                //Elliptical
                if (eccentricity < 1) {
                    return Math.Pow(a * a * a / this.standardGravitationalParameter, 0.5) * meanAnomaly;
                }
                //Parabolic
                else if (eccentricity == 1) {
                    return Math.Pow(2 * a * a * a / this.standardGravitationalParameter, 0.5) * meanAnomaly;
                }
                //Hyperbolic
                else {
                    return Math.Pow(-a * a * a / this.standardGravitationalParameter, 0.5) * meanAnomaly;
                }
            }
        }

        /// <summary>
        /// Length to the apoapsis (m)
        /// </summary>
        public double apoapsis {
            get {
                //Elliptical
                if (eccentricity < 1) {
                    return (1 + eccentricity) * a;
                }
                //Hyperbolic or Parabolic
                else {
                    return double.PositiveInfinity;
                }
            }
        }

        /// <summary>
        /// Period of the orbit "sidereal year" (s)
        /// </summary>
        public double period {
            get {
                //Elliptical
                if (eccentricity < 1) {
                    return KeplerConstants.TWO_PI * System.Math.Pow(a * a * a / this.standardGravitationalParameter, 0.5);
                }
                //Parabolic or hyperbolic
                else {
                    return double.PositiveInfinity;
                }
            }
        }

        /// <summary>
        /// Angular speed required for a body to complete one orbit (rad/s)
        /// </summary>
        public double meanAngularSpeed {
            get {
                if (eccentricity < 1) {
                    return Math.Pow(a * a * a / this.standardGravitationalParameter, -0.5);
                }
                else {
                    return double.PositiveInfinity;
                }
            }
        }

        /// <summary>
        /// Shortcut for meanAngularSpeed (rad/s)
        /// </summary>
        public double meanMotion {
            get {
                return this.meanAngularSpeed;
            }
        }

        /// <summary>
        /// The angle of the eccentric anomaly (rad)
        /// </summary>
        public double eccentricAnomaly {
            get {
                return KeplerUtils.MeanToEccentric(this.meanAnomaly, this.eccentricity);
            }
        }

        /// <summary>
        /// The angle of th eTrue anomaly (rad)
        /// </summary>
        public double trueAnomaly {
            get {
                return KeplerUtils.EccentricToTrue(this.eccentricAnomaly, this.eccentricity);
            }
        }

        #endregion

        #region constructors
        /// <summary>
        /// Construct kepler orbit from base parameters
        /// </summary>
        /// <param name="parent">Parent body</param>
        /// <param name="a">semi-major axis length</param> 
        /// <param name="e">eccentricity</param>
        /// <param name="ma">mean anomaly</param>
        /// <param name="i">inclination</param>
        /// <param name="w">perifocus</param>
        /// <param name="omega">ascending node</param>
        public KeplerOrbit(KeplerBody parent, double a, double e, double ma, double i, double w, double omega) {
            this.parent = parent;
            this.semiMajorLength = a;
            this.eccentricity = e;
            this.meanAnomaly = ma;
            this.inclination = i;
            this.perifocus = w;
            this.ascendingNode = omega;
        }

        /// <summary>
        /// Construct kepler orbit from orbital parameters object
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="parameters"></param>
        public KeplerOrbit(KeplerBody parent, KeplerOrbitalParameters parameters) : this(
            parent,
            parameters.semiMajorLength,
            parameters.eccentricity,
            parameters.meanAnomaly,
            parameters.inclination,
            parameters.perifocus,
            parameters.ascendingNode
        ) {}

        #endregion

        #region helper_functions

        /// <summary>
        /// Rotate a vector from the orbital XZ plane to world XYZ coordinates
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        private Vector3d undoRotation(Vector3d vec) {
            //Use the XZ plane with +X as the reference direction
            Vector3d rot = new Vector3d(vec);
            
            //Rotate to match inclination
            rot = Vector3d.Rotate(rot, this.inclination, Vector3d.forward);

            //Rotate to align the ascending node to the reference direction
            rot = Vector3d.Rotate(rot, this.ascendingNode, Vector3d.up);

            //Rotate the periapsis line
            rot = Vector3d.Rotate(rot, this.perifocus, this.normal);

            return rot;
        }

        #endregion

        #region manipulators

        /// <summary>
        /// Get the 2D position in the orbital XZ plane that represents the position of that body at Eccentric Anomaly E
        /// </summary>
        /// <param name="E"></param>
        /// <returns></returns>
        public Vector3d GetPlanarPositionAtEccentricAnomaly(double E) {
            double e = this.eccentricity;
            return new Vector3d(
                a * (Math.Cos(E) - e),
                0,
                a * Math.Sin(E) * Math.Sqrt(1 - e * e)
            );
        }

        /// <summary>
        /// Get the 3D position in the planet centered XYZ that represents the position of that body at Eccentric Anomaly E
        /// </summary>
        /// <param name="E"></param>
        /// <returns></returns>
        public Vector3d GetWorldPositionAtEccentricAnomaly(double E) {
            //Get the planar position
            Vector3d local = GetPlanarPositionAtEccentricAnomaly(E);

            //Rotate into world coordinates
            return undoRotation(local);
        }

        /// <summary>
        /// Get the 3D position in the planet centered XYZ coordiante system at the current position
        /// </summary>
        /// <returns></returns>
        public Vector3d GetCurrentPosition() {
            return this.GetWorldPositionAtEccentricAnomaly(this.eccentricAnomaly);
        }

        /*
        /// <summary>
        /// Add velocity to modify the orbit
        /// </summary>
        /// <param name="deltaV"></param>
        public void AddVelocity(Vector3d deltaV) {
            Vector3d vel = this.GetVelocity() + deltaV;

            KeplerOrbitalParameters kp = KeplerOrbitalParameters.SolveOrbitalParameters(this.standardGravitationalParameter, this.GetPosition(), vel);

            this.semiMajorLength = kp.semiMajorLength;
            this.eccentricity = kp.eccentricity;
            this.meanAnomaly = kp.meanAnomaly;
            this.inclination = kp.inclination;
            this.perifocus = kp.perifocus;
            this.ascendingNode = kp.ascendingNode;
        }*/

        /// <summary>
        /// Move the object forward or backwards in time by a fixed time-step
        /// </summary>
        public void StepTime(double deltaTime) {
            double mm = this.meanMotion;
            double deltaM = (deltaTime * mm) % (KeplerConstants.TWO_PI);
            double n_ma = (meanAnomaly + deltaM) % (KeplerConstants.TWO_PI);

            this.meanAnomaly = n_ma;
        }

        public override string ToString() {
            var obj = new {
                SemiMajorAxis = this.semiMajorLength,
                SemiMinorAxis = this.semiMinorAxis,
                Eccentricity = this.eccentricity,
                Inclination = this.inclination,
                ArgumentOfPeriapsis = this.perifocus,
                LongitudeOfAscendingNode = this.ascendingNode,
                OrbitalPeriod = this.period,
                MeanAnomaly = this.meanAnomaly,
                EccentricAnomaly = this.eccentricAnomaly,
                TrueAnomaly = this.trueAnomaly,
                PeriapsisDistance = this.periapsis,
                ApoapsisDistance = this.apoapsis,
                MeanMotion = this.meanMotion
            };
            return 
                "{\n" +
                "    \"SemiMajorAxis\": "+obj.SemiMajorAxis + "\n" +
                "    \"SemiMinorAxis\": " + obj.SemiMinorAxis + "\n" +
                "    \"Eccentricity\": " + obj.Eccentricity + "\n" +
                "    \"Inclination\": " + obj.Inclination + "\n" +
                "    \"ArgumentOfPeriapsis\": " + obj.ArgumentOfPeriapsis + "\n" +
                "    \"LongitudeOfAscendingNode\": " + obj.LongitudeOfAscendingNode + "\n" +
                "    \"OrbitalPeriod\": " + obj.OrbitalPeriod + "\n" +
                "    \"MeanAnomaly\": " + obj.MeanAnomaly + "\n" +
                "    \"EccentricAnomaly\": " + obj.EccentricAnomaly + "\n" +
                "    \"TrueAnomaly\": " + obj.TrueAnomaly + "\n" +
                "    \"PeriapsisDistance\": " + obj.PeriapsisDistance + "\n" +
                "    \"ApoapsisDistance\": " + obj.ApoapsisDistance + "\n" +
                "    \"MeanMotion\": " + obj.MeanMotion + "\n" +
                "    \"Position\": " + this.GetCurrentPosition().ToString() + "\n" +
                "}";
        }

        #endregion

    }

    /// <summary>
    /// Class used to describe a body of a given mass in a specific orbit
    /// </summary>
    public class KeplerBody {

        /// <summary>
        /// Mass of the body in KG
        /// </summary>
        public double mass {
            get; private set;
        }

        /// <summary>
        /// KeplerOrbit object representing the orbital path of this body
        /// </summary>
        public KeplerOrbit orbit {
            get; private set;
        }

        public KeplerBody(double mass, KeplerOrbit orbit){
            this.mass = mass;
            this.orbit = orbit;
        }

        public override string ToString() {
            return "{\n\"Mass\": " + this.mass + ",\n" + "\"Orbit\": " + this.orbit.ToString() + "\n}";
        }

    }

}
