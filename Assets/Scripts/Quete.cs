using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quete
{
    //Privates
    private string[] tags = { "Wolf", "Goblin", "Orc" }; //Liste des tags possibles
    private int[] dangerosites = { 4, 8, 10 }; //Liste des dangerosites de chacun des tags (meme ordre que les tags)
    private bool questDone; //Si la quete est accompli ou non
    private int totalKill; //Combien de kills il faut faire pour remplir la quete
    private int currentKill; //Combien de kills ont deja eu lieu
    private string tag; //Les cibles de la quete
    private int peril; //A quel point la quete est compliquee
    private int recompense; //L'exp qu'on gagne
    private Vector3 destination; //La ou on doit aller

    public Quete(int niveau)
    {
        questDone = false;

        //On genere le nombre de kills pour la quete
        currentKill = 0;
        totalKill = Random.Range(niveau, 3 * niveau + 1);

        //On trouve l'index du tag
        int index = Random.Range(0, 3);
        tag = tags[index];
        peril = dangerosites[index] * totalKill;

        //On calcule l'exp recompense
        float exp = 0;
        for (int i = 0; i < totalKill; i++) exp += Random.Range(3f, 4f) * dangerosites[index];
        recompense = (int)(exp);

        //On determine la ou on envoie les joueurs
        getNewDestination();
    }

    //Savoir sur qui on doit taper
    public string GetTag() => tag;

    //Savoir si la quete est finie ou pas
    public bool GetQuestDone() => questDone;

    //On change de destination
    public void getNewDestination()
    {
        GameObject[] mobs = GameObject.FindGameObjectsWithTag(tag);
        if (mobs.Length == 0) destination = Vector3.zero;
        else destination = mobs[Random.Range(0, mobs.Length)].GetComponentInParent<AMob>().GetSpawnerPosition();
    }

    //A quel point la quete est avancee
    public float getAvancement()
    {
        return ((float)(currentKill) / (float)(totalKill));
    }

    //Savoir ou on va
    public Vector3 getDestination() => destination;

    //Pour recuperer l'experience de la quete
    public int getRecompense() => recompense;

    //Savoir le peril de la quete pour discuter
    public int getPeril() => peril;

    //Lorsqu'on a tue un ennemi, check si il a le bon tag et l'avancement de la quete
    public void gotKilled(string victimTag)
    {
        //Si c'est le bon tag, on tient compte des victimes
        if (tag == victimTag)
        {
            currentKill++;
            if (currentKill == totalKill) questDone = true;
        }
    }

    //Obtenir l'etat de la quete pour l'UI
    public string GetQueteStatus() => currentKill + "/" + totalKill + " " + tag;
}
