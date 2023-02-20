using System;

namespace AutomotiveWorld.Models.Parts
{
    public enum EngineType
    {
        Unknown,

        /** InternalPetrolCombustionEngine
         * - low particulate emissions
         * - significant reduction in emissions for the Euro6 standard
         * - suitable for suburban travel and better tolerated in cities (LEZ zones)
         * - new engine technologies have reduced fuel consumption
         * - the fuel sold at the pump is lead-free and contains 10% bioethanol
        **/
        ESS,

        /** DieselInternalCombustionEngine
         * - fossil fuel emitting less CO2 than petrol (-10%)
         * - Euro6d standard ensures compliance with emission standards
         * - LEZs will accept Euro6d Diesel until 2030
         * - always an interesting choice if you make long regular journeys and a large annual mileage
         * - Possibility of replacing fossil diesel with e-Diesel
         * - fuel sold at the pump contains biodiesel
        **/
        DSL,

        /** Mild hybrid electric vehicle
         * - power assist provides a boost to the combustion engine when starting and accelerating
         * - very small battery with no need to recharge (energy recovery during deceleration)
         * - engine cut-off at standstill and coasting at idle
         * - some more efficient systems use a voltage of 48V
         * - reduces fuel consumption while maintaining the flexibility of the conventional internal combustion engine
        **/
        MHEV,

        /** Hybrid electric vehicle
         * - an electric motor with a small battery helps the combustion engine
         * - the battery is recharged by recovering energy during braking and deceleration
         * - the electric engine can be used on its own to power the car at low speeds (30 zones, manoeuvres, traffic jams)
         * - runs 50% of the time on electric power in the city
         * - very easy to use and very flexible
         * - lower CO2 emissions and reduced consumption
         **/
        HEV,

        /** Plug-in hybrid electric vehicle
         * - combines the advantages of an electric car and a combustion engine car
         * - can be driven in electric, combustion engine or hybrid mode (both combined)
         * - makes it possible to drive in the city without emitting CO2
         * - electricity range is sometimes more than 50km, or even 100km
         * - very easy to charge (takes less than one night when plugged into a wall socket)
         * - flexible and easy to use, without the stress of running out of charge
        **/
        PHEV,

        /** Compressed natural gas engine
         * - fuel composed mainly of methane, natural gas
         * - can be completely decarbonised (e-CNG)
         * - always has an engine capable of driving on petrol (fuel reserve so you can continue without CNG)
         * - accepted in underground car parks
         * - less polluting and more economical (10% less CO2 than petrol)
        **/
        CNG,

        /** Liquefied petroleum gas engine
         * - gases consisting of propane and butane recovered during the distillation of crude oil
         * - with the new R67-01 standards, it is accepted in underground car parks
         * - no benzene and lower emissions of NOx, CO, particulate matter and unburned hydrocarbons
         * - very cheap fuel and a very good network of pumps
        **/
        LPG,

        /** 100% electric motor (battery electric vehicle
         * - no engine emissions
         * - range depends on the model type; sometimes more than 400km
         * - possible to charge it at home
         * - (super)fast network of charging stations being fully developed (around 10,000 charging points in Belgium)
         * - very smooth, comfortable and quiet ride
        **/
        BEV,

        /** Fuel cell electric vehicle
         * - uses hydrogen to produce electricity to power an electric motor
         * - range and fast to recharge (fill with hydrogen) 
         * - only releases water
         * - no heavy battery
         * - network of stations under development in Belgium (10 points by 2022)
        **/
        FCEV
    }
}
