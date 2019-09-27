using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

/// @author Hannamari Heiniluoma
/// @version 21.11.2018
/// <summary>
/// Pieni peli, jossa kerätään lumihiutaleita.
/// </summary>
public class Lumihiutale : PhysicsGame
{
    private IntMeter laskuri;
    private Image[] hiutaleet = { LoadImage("lumihiutale2"), LoadImage("lumihiutale3"), LoadImage("lumihiutale4") };
    private PhysicsObject alareuna;
    private PlatformCharacter pelaaja;
    private double nopeus = 200.0;
    private EasyHighScore topLista = new EasyHighScore();

    /// <summary>
    /// Tästä ohjelma alkaa. Luodaan erillisillä aliohjelmilla kenttä ja asetetaan ohjaimet.
    /// </summary>
    public override void Begin()
    {
        LuoKentta();
        AsetaOhjaimet();
    }   


    /// <summary>
    /// Luodaan kentälle kaikki tarvittava.
    /// </summary>
    private void LuoKentta()
    {
        ClearAll();
        Camera.ZoomToLevel();
        Level.Background.Color = new Color(26, 59, 112);
        alareuna = Level.CreateBottomBorder();
        Level.CreateLeftBorder();
        Level.CreateRightBorder();
        Level.CreateTopBorder();

        Gravity = new Vector(0, -100);

        LuoLaskuri();
        LuoPelaaja();
        LuoViholliset(2);

        Timer t = new Timer();
        t.Interval = 0.5;
        t.Timeout += LuoLumihiutale;
        t.Start();

        IsPaused = false;
    }


    /// <summary>
    /// Asetetaan ohjaimet.
    /// </summary>
    private void AsetaOhjaimet()
    {
        Keyboard.Listen(Key.Up, ButtonState.Down, delegate { pelaaja.Jump(nopeus); }, "Pelaaja hyppää ylöspäin");
        Keyboard.Listen(Key.Right, ButtonState.Down, delegate { pelaaja.Walk(nopeus); }, "Pelaaja liikkuu oikealle");
        Keyboard.Listen(Key.Left, ButtonState.Down, delegate { pelaaja.Walk(-nopeus); }, "Pelaaja liikkuu vasemmalle");

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Luodaan pistelaskuri ja sitä varten pistenäyttö, joka sijoitetaan näytön vasempaan yläkulmaan.
    /// </summary>
    private void LuoLaskuri()
    {
        laskuri = new IntMeter(0);
        Label pistenaytto = new Label();
        pistenaytto.X = Screen.Left + 60;
        pistenaytto.Y = Screen.Top - 20;
        pistenaytto.TextColor = Color.White;
        pistenaytto.Width = 100;
        pistenaytto.BindTo(laskuri);
        Add(pistenaytto);
    }


    /// <summary>
    /// Luodaan tasohyppelyolio, jota pelaaja ohjaa.
    /// </summary>
    private void LuoPelaaja()
    {
        pelaaja = new PlatformCharacter(2 * 50.0, 2 * 80.0, Shape.Rectangle);
        pelaaja.Y = Level.Bottom + pelaaja.Height/2;
        pelaaja.Image = LoadImage("lapsi");
        Add(pelaaja);
        AddCollisionHandler(pelaaja, "vihollinen", PeliPaattyi);
        AddCollisionHandler(pelaaja, "hiutale", LumihiutaleNapattiin);
        Timer.CreateAndStart(0.01, () =>
        {
            if (pelaaja.Bottom > alareuna.Top)
                pelaaja.Y -= 2;
        });
    }


    /// <summary>
    /// Luodaan viholliset, joihin pelaaja ei saa osua.
    /// </summary>
    /// <param name="maara">vihollisten määrä</param>
    private void LuoViholliset(int maara)
    {
        for (int i = 1; i <= maara; i++)
        {
            PhysicsObject teline = PhysicsObject.CreateStaticObject(2 * 50.0, 2 * 100.0, Shape.Rectangle);
            teline.Y = Level.Bottom + 100;
            if (i % 2 == 0)
            {
                teline.X = RandomGen.NextDouble(Level.Right - 400.0, Level.Right - teline.Width/2);
            }
            else
            {
                teline.X = RandomGen.NextDouble(Level.Left + teline.Width / 2, Level.Left + 400.0);
            }
            teline.Image = LoadImage("mattoteline");
            teline.Tag = "vihollinen";
            Add(teline);
        }
    }


    /// <summary>
    /// Luodaan lumihiutale kentän ylälaitaan, valitaan kuva satunnaisesti taulukosta.
    /// </summary>
    private void LuoLumihiutale()
    {
        PhysicsObject hiutale = new PhysicsObject(2 * 10.0, 2 * 10.0, Shape.Circle);
        hiutale.Image = hiutaleet[RandomGen.NextInt(0, hiutaleet.Length-1)];
        hiutale.Y = Level.Top - hiutale.Height/2;
        hiutale.X = RandomGen.NextDouble(Level.Left + hiutale.Width/2, Level.Right - hiutale.Width / 2);
        hiutale.CanRotate = false;
        hiutale.Tag = "hiutale";
        AddCollisionHandler(hiutale, alareuna, HiutalePutosiMaahan);
        AddCollisionHandler(hiutale, "vihollinen", HiutalePutosiMaahan);
        Add(hiutale);    
    }


    /// <summary>
    /// Tuhoaa hiutaleen, kun se osuu maahan tai mattotelineeseen.
    /// </summary>
    /// <param name="hiutale">hiutale</param>
    /// <param name="jokumuu">kentän alareuna tai mattoteline</param>
    private void HiutalePutosiMaahan(PhysicsObject hiutale, PhysicsObject jokumuu)
    {
        Remove(hiutale);
    }


    /// <summary>
    /// Aliohjelma, joka suoritetaan jos pelaaja ja hiutale törmäävät.
    /// </summary>
    /// <param name="pelaaja">pelaaja</param>
    /// <param name="hiutale">lumihiutale</param>
    private void LumihiutaleNapattiin(PhysicsObject pelaaja, PhysicsObject hiutale)
    {
        Remove(hiutale);
        laskuri.Value++;
    }


    /// <summary>
    /// Aliohjelma, joka suoritetaan jos pelaaja ja vihollinen törmäävät. Peli pysähtyy ja näytetään toplista.
    /// </summary>
    /// <param name="pelaaja">pelaaja</param>
    /// <param name="vihollinen">mattoteline</param>
    private void PeliPaattyi(PhysicsObject pelaaja, PhysicsObject vihollinen)
    {
        IsPaused = true;
        topLista.EnterAndShow(laskuri.Value);
        topLista.HighScoreWindow.Closed += NaytaValikko;        
    }


    /// <summary>
    /// Näytetään monivalintavalikko.
    /// </summary>
    /// <param name="sender">ikkuna josta tullaan</param>
    private void NaytaValikko(Window sender)
    {
        MultiSelectWindow valikko = new MultiSelectWindow("Jäit jumiin!",
        "Aloita uusi peli", "Parhaat pisteet", "Lopeta");
        valikko.ItemSelected += PainettiinValikonNappia;
        Add(valikko);
    }


    /// <summary>
    /// Toimitaan sen mukaan, mitä käyttäjä valikosta valitsee.
    /// </summary>
    /// <param name="valinta">nappulan indeksi</param>
    private void PainettiinValikonNappia(int valinta)
    {
        switch (valinta)
        {
            case 0:
                Begin();
                break;
            case 1:
                topLista.Show();
                break;
            case 2:
                Exit();
                break;
        }
    }
}
