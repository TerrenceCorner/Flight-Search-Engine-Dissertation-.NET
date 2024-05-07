using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using WebApplication9.Pages;
using System.IO;
using WebApplication9.Pages;
using Newtonsoft.Json;
using System.Text;

namespace WebApplication9.Pages
{

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;

    public class FlightGenerator
    {
        private List<string> airports;
        private List<string> airlines;
        private List<Dictionary<string, object>> flights;

        public FlightGenerator()
        {
            airports = new List<string>
        {
            "London", "Paris", "Berlin", "Porto", "Istanbul", "Amsterdam", "Geneva", "Brussels"
        };

            airlines = new List<string>
        {
            "Ryanair", "Wizzair", "Easyjet", "Lufthansa", "Emirates", "Loganair"
        };

            flights = new List<Dictionary<string, object>>();
        }

        public void GenerateFlights()
        {
            DateTime end = DateTime.Now.AddMonths(1);

            for (DateTime date = DateTime.Now; date <= end; date = date.AddDays(1))
            {
                foreach (string origin in airports)
                {
                    foreach (string destination in airports)
                    {
                        if (origin != destination)
                        {
                            int numFlights = new Random().Next(1, 4);
                            for (int i = 0; i < numFlights; i++)
                            {
                                Dictionary<string, object> flight = GenerateFlight(date, origin, destination);
                                flights.Add(flight);
                            }
                        }
                    }
                }
            }

            string jsonFlightData = JsonSerializer.Serialize(flights, new JsonSerializerOptions { WriteIndented = true });

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pages", "flights.json");

            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.Write(jsonFlightData);
            }
        }

        private Dictionary<string, object> GenerateFlight(DateTime date, string origin, string destination)
        {
            int departureHour = new Random().Next(5, 24);
            int departureMinute = new Random().Next(0, 60);

            DateTime departureTime = new DateTime(date.Year, date.Month, date.Day, departureHour, departureMinute, 0);

            int flightTimeHour = new Random().Next(1, 6);
            int flightTimeMinutes = new Random().Next(0, 60);

            int arrivalHour = (departureHour + flightTimeHour) % 24;
            int arrivalMinute = (departureMinute + flightTimeMinutes) % 60;

            string arrivalTime = $"{arrivalHour:D2}:{arrivalMinute:D2}";
            string duration = $"{flightTimeHour}:{flightTimeMinutes:D2}";

            double cabinBagCostDouble = new Random().NextDouble() * (15 - 7.5) + 7.5;
            long cabinBagCost = (long)Math.Round(cabinBagCostDouble);
            double checkedBagCost = new Random().Next(15, 26);

            int index = new Random().Next(0, airlines.Count);
            string airline = airlines[index];

            long premiumEcon, business, first;

            if (airline == "Emirates")
            {
                premiumEcon = 3;
                business = 4;
                first = 10;
            }
            else if (airline == "Lufthansa")
            {
                premiumEcon = 2;
                business = 3;
                first = 8;
            }
            else
            {
                premiumEcon = 1;
                business = 1;
                first = 1;
            }

            int price = new Random().Next(20, 201);

            Dictionary<string, object> flight = new Dictionary<string, object>
        {
            { "departure_time", departureTime.ToString("yyyy.MM.dd HH:mm") },
            { "origin", origin },
            { "destination", destination },
            { "duration", duration },
            { "arrival_time", arrivalTime },
            { "airline", airline },
            { "price", price },
            { "checked_bag_cost", checkedBagCost },
            { "cabin_bag_cost", cabinBagCost },
            { "premium_econ_multiplier", premiumEcon },
            { "business_multiplier", business },
            { "first_multiplier", first }
        };

            return flight;
        }
    }
}

public class FlightSearchEngine : PageModel
{
    protected List<string> airports;
    private FlightGenerator flightGenerator;
    private FlightSearchFormHandler form;
    private List<Dictionary<string, object>> flightPairs;

    public FlightSearchEngine()
    {
        airports = new List<string>
        {
            "London", "Paris", "Berlin", "Porto", "Istanbul", "Amsterdam", "Geneva", "Brussels"
        };
        
        flightGenerator = new FlightGenerator();
}

    public void SetFlightPairs(List<Dictionary<string, object>> flightPairs)
    {
        this.flightPairs = flightPairs;
    }

    public List<string> GetAirports()
    {
        return airports;
    }

    public bool GetHasData()
    {
        if (form == null)
        {
            return false;
        }
        
        return form.GetHasData();
    }

    public FlightSearchFormHandler GetFlightSearchFormHandler()
    {
        return form;
    }

    public List<Dictionary<string, object>> GetFlights()
    {
        return flightPairs;
    }

    public List<Dictionary<string, object>> GetFlightPairs()
    {
        return flightPairs;
    }

    public List<Dictionary<string, object>> GetOutboundFlights()
    {
        return flightPairs.Select(pair => pair["outbound"] as Dictionary<string, object>).ToList();
    }

    public List<Dictionary<string, object>> GetReturnFlights()
    {
        return flightPairs.Select(pair => pair["return"] as Dictionary<string, object>).ToList();
    }

    public void AddFlightPair(Dictionary<string, object> flight)
    {
        flightPairs.Add(flight);
    }

    public void AddOutboundFlight(Dictionary<string, object> flight)
    {
        flightPairs.Add(flight);
    }

    public void OnPost()
    {
        form = new FlightSearchFormHandler(Request.Form["deptAirport"], Request.Form["destAirport"], Request.Form["departDay"], Request.Form["returnDay"], Request.Form["flightClass"], int.Parse(Request.Form["cabinBags"]), int.Parse(Request.Form["checkedBags"]), DateTime.Parse(Request.Form["earlyDept"]), DateTime.Parse(Request.Form["lateReturn"]), Request.Form["sort"] == "on" ? true : false);
        form.SetHasData(true);
        flightGenerator.GenerateFlights();
        this.InitializeFlights();
        this.SortPrice();

    }

    public void FlightSearch(string origin, string destination, DateTime date)
    {
        foreach (var flight in GetFlights())
        {
            var departureTime = DateTime.ParseExact((string)flight["departure_time"], "yyyy.MM.dd HH:mm", null);
            var flightDate = departureTime.Date;
            var originAirport = (string)flight["origin"];
            var destinationAirport = (string)flight["destination"];

            if (flightDate == date.Date && originAirport.Trim() == origin.Trim() && destinationAirport.Trim() == destination.Trim())
            {
                flight["duration"] = "(" + flight["duration"] + ")";
                AddFlightPair(flight);
            }
        }
    }

    public void InitializeFlights()
    {

        
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pages", "flights.json");
        string json;
        using (StreamReader sr = new StreamReader(filePath))
        {
            json = sr.ReadToEnd();
        }

        List<Flight> flights = new List<Flight>();

        foreach (var flightData in JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json))
        {
            DateTime departureTime = DateTime.ParseExact((string)flightData["departure_time"], "yyyy.MM.dd HH:mm", null);
            string origin = (string)flightData["origin"];
            string destination = (string)flightData["destination"];
            string duration = (string)flightData["duration"];
            string arrivalTime = (string)flightData["arrival_time"];
            string airline = (string)flightData["airline"];
            long price = (long)flightData["price"];
            long checkedBagCost = (long)flightData["checked_bag_cost"];
            long cabinBagCost = (long)flightData["cabin_bag_cost"];
            long premiumEconMultiplier = (long)flightData["premium_econ_multiplier"];
            long businessMultiplier = (long)flightData["business_multiplier"];
            long firstMultiplier = (long)flightData["first_multiplier"];

            Flight flight = new Flight(departureTime, origin, destination, duration, arrivalTime, airline, price, checkedBagCost, cabinBagCost, premiumEconMultiplier, businessMultiplier, firstMultiplier);
            flights.Add(flight);
        }

        List<Flight> outboundFlights = new List<Flight>();
        List<Flight> returnFlights = new List<Flight>();

        var searchCriteria = GetFlightSearchFormHandler();

        foreach (var flight in flights)
        {
            DateTime departureTime = flight.GetDepartureTime();
            var flightDate = departureTime.Date;
            var originAirport = flight.GetOrigin();
            var destinationAirport = flight.GetDestination();

            var departDateString = searchCriteria.GetEarlyDept().ToString("yyyy.MM.dd");
            var returnDateString = searchCriteria.GetLateReturn().ToString("yyyy.MM.dd");

            if (flightDate == DateTime.Parse(departDateString).Date && originAirport.Trim() == searchCriteria.GetDeptAirport().Trim() && destinationAirport.Trim() == searchCriteria.GetDestAirport().Trim())
            {
                flight.SetDuration("(" + flight.GetDuration() + ")");
                outboundFlights.Add(flight);
            }

            if (flightDate == DateTime.Parse(returnDateString).Date && originAirport.Trim() == searchCriteria.GetDestAirport().Trim() && destinationAirport.Trim() == searchCriteria.GetDeptAirport().Trim())
            {
                flight.SetDuration("(" + flight.GetDuration() + ")");
                returnFlights.Add(flight);
            }
        }

        var flightPairs = new List<Dictionary<string, object>>();
        foreach (var outboundFlight in outboundFlights)
        {
            foreach (var returnFlight in returnFlights)
            {
                var price = (outboundFlight.GetPrice() + returnFlight.GetPrice());
                var pair = new Dictionary<string, object>
                {
                    { "outbound", outboundFlight },
                    { "return", returnFlight },
                    { "price", price }
                };
                flightPairs.Add(pair);
            }
        }

        SetFlightPairs(flightPairs);
    }

    public List<Dictionary<string, object>> ProcessOutboundFlightData(bool departFound)
    {
        var outboundFlights = new List<Dictionary<string, object>>();
        foreach (var flight in GetFlights())
        {
            var outboundPrice = CalculateBaggageFees((int)flight["price"], flight);
            outboundPrice = CalculateClassFees(outboundPrice, flight);
            outboundFlights.Add(new Dictionary<string, object>
            {
                { "outbound", flight },
                { "outboundPrice", outboundPrice }
            });
        }

        departFound = false;
        return outboundFlights;
    }

    public List<Dictionary<string, object>> ProcessReturnFlightData(bool returnFound)
    {
        var returnFlights = new List<Dictionary<string, object>>();
        foreach (var flight in GetFlights())
        {
            var returnPrice = CalculateBaggageFees((int)flight["price"], flight);
            returnPrice = CalculateClassFees(returnPrice, flight);
            returnFlights.Add(new Dictionary<string, object>
            {
                { "return", flight },
                { "returnPrice", returnPrice }
            });
        }

        returnFound = false;
        return returnFlights;
    }

    public double CalculateBaggageFees(int baseFare, Dictionary<string, object> flight)
    {
        var fare = (double)baseFare;

        if (form.GetCheckedBags() > 1)
        {
            fare += form.GetCheckedBags() * (double)flight["checked_bag_cost"];
        }

        if (form.GetCabinBags() > 1)
        {
            fare += form.GetCabinBags() * (double)flight["cabin_bag_cost"];
        }

        return fare;
    }

    public void SortPrice()
    {
        if (form.GetSort() != null && form.GetSort() == true)
        {
            flightPairs = flightPairs.OrderBy(pair => pair["price"]).ToList();
        }
    }

    public double CalculateClassFees(double baseFare, Dictionary<string, object> flight)
    {
        var fare = baseFare;
        if (form.GetFlightClass() != null)
        {
            if (form.GetFlightClass() == "premEcon")
            {
                fare *= (long)flight["premium_econ_multiplier"];
            }
            else if (form.GetFlightClass() == "business")
            {
                fare *= (long)flight["business_multiplier"];
            }
            else if (form.GetFlightClass() == "first")
            {
                fare *= (long)flight["first_multiplier"];
            }
        }

        return fare;
    }

    public string GetFlightSearchResults()
    {
        StringBuilder htmlBuilder = new StringBuilder();

        if (flightPairs.Any())
        {
            foreach (var pair in flightPairs)
            {
                var outboundFlight = (Flight)pair["outbound"];
                var returnFlight = (Flight)pair["return"];
                var price = (long)pair["price"];

                htmlBuilder.Append("<div class=\"container\">");
                htmlBuilder.Append("<div class=\"text-box\">");
                htmlBuilder.Append("<p class=\"text\">");
                htmlBuilder.Append(PrintFlightDetails(outboundFlight));
                htmlBuilder.Append(" | ");
                htmlBuilder.Append(PrintFlightDetails(returnFlight));
                htmlBuilder.Append($" ${price}");
                htmlBuilder.Append("</p>");
                htmlBuilder.Append("</div>");
                htmlBuilder.Append("</div>");
            }
        }
        else
        {
            htmlBuilder.Append("No flights matching this criteria found");
        }

        return htmlBuilder.ToString();
    }

    private string PrintFlightDetails(Flight flight)
    {
        return $"Departure: {flight.GetDepartureTime()}, " +
               $"Origin: {flight.GetOrigin()}, " +
               $"Destination: {flight.GetDestination()}, " +
               $"Duration: {flight.GetDuration()}, " +
               $"Arrival: {flight.GetArrivalTime()}, " +
               $"Airline: {flight.GetAirline()}<br>";
    }
}

public class Flight
{
    private DateTime departureTime;
    private string origin;
    private string destination;
    private string duration;
    private string arrivalTime;
    private string airline;
    private long price;
    private long checkedBagCost;
    private long cabinBagCost;
    private long premiumEconMultiplier;
    private long businessMultiplier;
    private long firstMultiplier;

    public Flight(DateTime departureTime, string origin, string destination, string duration, string arrivalTime, string airline, long price, long checkedBagCost, long cabinBagCost, long premiumEconMultiplier, long businessMultiplier, long firstMultiplier)
    {
        this.departureTime = departureTime;
        this.origin = origin;
        this.destination = destination;
        this.duration = duration;
        this.arrivalTime = arrivalTime;
        this.airline = airline;
        this.price = price;
        this.checkedBagCost = checkedBagCost;
        this.cabinBagCost = cabinBagCost;
        this.premiumEconMultiplier = premiumEconMultiplier;
        this.businessMultiplier = businessMultiplier;
        this.firstMultiplier = firstMultiplier;
    }

    public DateTime GetDepartureTime()
    {
        return departureTime;
    }

    public string GetOrigin()
    {
        return origin;
    }

    public string GetDestination()
    {
        return destination;
    }

    public string GetDuration()
    {
        return duration;
    }

    public string GetArrivalTime()
    {
        return arrivalTime;
    }

    public string GetAirline()
    {
        return airline;
    }

    public long GetPrice()
    {
        return price;
    }

    public double GetCheckedBagCost()
    {
        return checkedBagCost;
    }

    public long GetCabinBagCost()
    {
        return cabinBagCost;
    }

    public long GetPremiumEconMultiplier()
    {
        return premiumEconMultiplier;
    }

    public long GetBusinessMultiplier()
    {
        return businessMultiplier;
    }

    public long GetFirstMultiplier()
    {
        return firstMultiplier;
    }

    public void SetDuration(string duration)
    {
        this.duration = duration;
    }

    public void PrintFlightDetails()
    {
        Console.WriteLine(departureTime);
        Console.WriteLine(origin);
        Console.WriteLine("->");
        Console.WriteLine(destination);
        Console.WriteLine(duration);
        Console.WriteLine(arrivalTime);
        Console.WriteLine(airline);
    }
}

    public class FlightSearchFormHandler
{
        private bool sort = false;
        private bool hasData = false;
        private string deptAirport = "";
        private string destAirport = "";
        private string departDay = "";
        private string returnDay = "";
        private string flightClass = "";
        private int cabinBags = 0;
        private int checkedBags = 0;
        private DateTime earlyDept = DateTime.Now;
        private DateTime lateReturn = DateTime.Now;

    public FlightSearchFormHandler(string deptAirport, string destAirport, string departDay, string returnDay, string flightClass, int cabinBags, int checkedBags, DateTime earlyDept, DateTime lateReturn, bool sort)
    {
        this.sort = false;
        this.deptAirport = deptAirport;
        this.destAirport = destAirport;
        this.departDay = departDay;
        this.returnDay = returnDay;
        this.flightClass = flightClass;
        this.cabinBags = cabinBags;
        this.checkedBags = checkedBags;
        this.earlyDept = earlyDept;
        this.lateReturn = lateReturn;
    }

    public void SetHasData(bool value)
        {
            hasData = value;
        }

        public void SetDeptAirport(string value)
        {
            deptAirport = value;
        }

        public void SetDestAirport(string value)
        {
            destAirport = value;
        }

        public void SetDepartDay(string value)
        {
            departDay = value;
        }

        public void SetReturnDay(string value)
        {
            returnDay = value;
        }

        public void SetFlightClass(string value)
        {
            flightClass = value;
        }

        public void SetCabinBags(int value)
        {
            cabinBags = value;
        }

        public void SetCheckedBags(int value)
        {
            checkedBags = value;
        }

        public void SetEarlyDept(DateTime value)
        {
            earlyDept = value;
        }

        public void SetLateReturn(DateTime value)
        {
            lateReturn = value;
        }

        public bool GetHasData()
        {
            return hasData;
        }

    public bool GetSort()
    {
        return sort;
    }

    public string GetDeptAirport()
    {
        return deptAirport;
    }

    public string GetDestAirport()
    {
        return destAirport;
    }

    public string GetDepartDay()
    {
        return departDay;
    }

    public string GetReturnDay()
    {
        return returnDay;
    }

    public string GetFlightClass()
    {
        return flightClass;
    }

    public int GetCabinBags()
    {
        return cabinBags;
    }

    public int GetCheckedBags()
    {
        return checkedBags;
    }

    public DateTime GetEarlyDept()
    {
        return earlyDept;
    }

    public DateTime GetLateReturn()
    {
        return lateReturn;
    }

}
