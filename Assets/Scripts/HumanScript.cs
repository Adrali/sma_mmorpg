using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HumanScript : MonoBehaviour, Damageable
{
    //Enums
    protected enum etat
    {
        Walk, //On va vers notre destination, le lieu de quete
        Return, //On va vers le panneau de quete
        Rush, //On cherche a engager l'ennemi
        Attack, //On cherche a taper l'ennemi
        Dodge, //On veut esquiver le combat a tout prix
        Wait //On fait une pause au panneau, pour discuter avec les joueurs et voir ce qu'il se passe
    }

    //Constantes
    private const float AttackCooldown = 1.0f; //Le delai entre chaque attaque

    //Private
    private Vector3 destination = Vector3.zero; //La ou on va
    private Rigidbody target = null; //Celui sur qui on tape
    private Rigidbody closestMonster = null; //Le monstre le plus proche de nous parmis ceux qu'on voit
    private float closestMonsterDistance = 1000;
    private etat currentEtat; //Notre etat actuel
    private Quete currentQuete; //La quete qu'on veut remplir

    private float speed = 4.2f; //La vitesse au sol de l'humain

    private int niveau = 1; //Le niveau actuel de notre humain
    private int pointsDeVie; //Nombre maximal de points de vie actuel
    private int currentPointsDeVie; //Nombre actuel de points de vie
    private int damages; //A quel point on tape fort
    private float attackTimer = 0f; //Combien de temps depuis la derniere attaque ?
    private int experience = 0; //Combien d'experience on a actuellement

    private List<AMob> monstresAutour = new List<AMob>(); //La liste des monstres autour du joueur, qu'il peut voir
    private List<HumanScript> humainsAutour = new List<HumanScript>(); //La liste des autres joueurs visibles
    private float waitTimer; //Combien de temps il nous reste a attendre avant de partir (pendant ce temps on parle)
    private float askTimer = 1f; //Combien de temps il faut attendre avant qu'on puisse faire une nouvelle demande d'ami
    private HumanScript copain = null; //Notre pote de coop

    //Public
    public Rigidbody humanRigidbody; //Utilise pour le deplacement et tout
    public Transform questBoard; //La ou se trouve le panneau de quete
    public LayerMask humanLayer; //Les autres humains
    public LayerMask mobLayer; //Les monstres a tuer
    public Text etatUI; //Etat actuel de l'humain
    public Text queteUI; //Etat actuel de la quete
    public Text PdVUI; //Pour montrer combien de pv il nous reste
    public MeshRenderer Tshirt; //Les vetements de l'humain, pour les changer

    private void Start()
    {
        //On genere aleatoirement nos stats pour notre humain
        pointsDeVie = Random.Range(30, 61);
        currentPointsDeVie = pointsDeVie;
        damages = Random.Range(5, 12);

        //On genere sa quete et on recupere ou on va
        currentQuete = new Quete(niveau);
        waitTimer = currentQuete.getPeril() / Random.Range(2f, 5f);
        askTimer += Random.Range(-0.5f, 0.5f);
        currentEtat = etat.Wait;

        //On trouve sa couleur de Tshirt
        Tshirt.material.SetColor("_Color", Color.black);
    }

    public HumanScript ReceiveCoopDemand(Quete newQuete, HumanScript autreHumain)
    {
        //On refuse toute demande si on a fini notre quete ou si on a un pote, et plus on est loin dans notre quete plus on a de chances de refuser
        if (!currentQuete.GetQuestDone() && copain == null && Random.Range(0f, 1f) > currentQuete.getAvancement())
        {
            //Si notre quete garantit le level up on ecoute jamais de propositions
            if (currentQuete.getRecompense() + experience >= niveau * 100) return null;
            //Sinon, si la quete de l'autre garantit le level up, on l'accepte si elle plus "rentable" que la notre
            else if (newQuete.getRecompense() + experience >= niveau * 100)
            {
                if ((float)newQuete.getRecompense() / newQuete.getPeril() >= (float)currentQuete.getRecompense() / currentQuete.getPeril())
                {
                    copain = autreHumain;
                    currentEtat = etat.Walk;
                    return this;
                }
                else return null;
            }
            //Sinon, on est interesse que si la quete est plus facile
            else if (newQuete.getPeril() < currentQuete.getPeril())
            {
                copain = autreHumain;
                currentEtat = etat.Walk;
                return this;
            }
            else return null;
        }
        else return null;
    }

    //Utiliser par les gens qui veulent nous donner une nouvelle quete
    public void GetNewQuete(Quete newQuete)
    {
        currentQuete = newQuete;
    }

    //Utiliser par les gens qui veulent nous donner une nouvelle cible
    public void GetNewTarget(Rigidbody newTarget)
    {
        target = newTarget;
        if (target == null) currentEtat = etat.Walk;
        else currentEtat = etat.Rush;
    }

    //Utilise pour recevoir un nouveau TShirt lorsqu'on rentre dans une equipe
    public void GetNewTShirt(Color couleur)
    {
        Tshirt.material.SetColor("_Color", couleur);
    }

    private void gagnerNiveau()
    {
        //On gagne en stats
        pointsDeVie += Random.Range(5, 11);
        currentPointsDeVie = pointsDeVie;
        damages += Random.Range(1, 4);
        //On perd en experience mais on gagne en niveau
        experience -= niveau * 100;
        if (experience < 0) experience = 0;
        niveau++;
    }

    private void FixedUpdate()
    {
        VerifierMonstres();
        VerifierHumains();

        if (attackTimer > 0) attackTimer -= Time.fixedDeltaTime; //On reduit le timer d'attaque en permanence
        etatUI.text = currentEtat.ToString();
        queteUI.text = currentQuete.GetQueteStatus() + " niveau " + niveau + " (" + (experience/niveau) + "%)";
        PdVUI.text = currentPointsDeVie + "/" + pointsDeVie;

        switch (currentEtat)
        {
            case etat.Walk:
                //Si la quete est fini on a juste a return
                if (currentQuete.GetQuestDone()) currentEtat = etat.Return;
                destination = currentQuete.getDestination();
                //On regarde dans le bon sens
                transform.LookAt(destination);
                //Si on est proche de la destination, on en trouve une autre
                if (Vector3.Distance(destination, humanRigidbody.position) <= 0.5f)
                {
                    currentQuete.getNewDestination();
                    if (copain != null) copain.GetNewQuete(currentQuete);
                }
                //On se deplace
                humanRigidbody.position += (destination - humanRigidbody.position).normalized * speed * Time.fixedDeltaTime;
                //On verifie si il y a un monstre trop proche qu'il faut fuir
                GetClosestMonster();
                if (closestMonsterDistance < 2.9f) currentEtat = etat.Dodge;
                break;

            case etat.Return:
                //Si on est assez proche du panneau on agit en consequence
                if (Vector3.Distance(questBoard.position, humanRigidbody.position) <= 3f) CloseToQuestBoard();
                //On regarde vers le panneau
                transform.LookAt(questBoard.position);
                //On va vers le panneau
                humanRigidbody.position += (questBoard.position - humanRigidbody.position).normalized * speed * Time.fixedDeltaTime;
                //On verifie si il y a un monstre trop proche qu'il faut fuir
                GetClosestMonster();
                if (closestMonsterDistance < 2.9f) currentEtat = etat.Dodge;
                break;

            case etat.Rush:
                //Si la quete est fini on a juste a return
                if (currentQuete.GetQuestDone()) currentEtat = etat.Return;
                //On regarde dans le bon sens
                transform.LookAt(target.position);
                if (Vector3.Distance(target.position, humanRigidbody.position) < 1.4f) currentEtat = etat.Attack; //Si on est assez proche on s'arrete et on tape
                else humanRigidbody.position += (target.position - humanRigidbody.position).normalized * speed * Time.fixedDeltaTime; //Sinon, on avance vers la cible
                //On verifie si il y a un monstre trop proche qu'il faut fuir
                GetClosestMonster();
                if (closestMonsterDistance < 2.9f) currentEtat = etat.Dodge;
                break;

            case etat.Attack:
                //Si la quete est fini on a juste a return
                if (currentQuete.GetQuestDone()) currentEtat = etat.Return;
                //On regarde dans le bon sens
                transform.LookAt(target.position);
                if (Vector3.Distance(target.position, humanRigidbody.position) > 1.6f) currentEtat = etat.Rush; //Si la cible s'est eloignee il faut la pourchasser
                else if (attackTimer <= 0f) //Si on a le bon timing, on peut attaquer
                {
                    target.GetComponent<Damageable>().TakeDamage(damages, this);
                    attackTimer = AttackCooldown;
                }
                break;

            case etat.Dodge:
                //On trouve le nouveau monstre le plus proche
                GetClosestMonster();
                if(closestMonster != null)
                {
                    //On regarde dans le bon sens
                    transform.LookAt(-closestMonster.position);
                    //On fuit le monstre le plus proche
                    humanRigidbody.position += (humanRigidbody.position - closestMonster.position).normalized * speed * Time.fixedDeltaTime;
                }
                //Si le monstre est assez loin, on trouve dans quel etat on est sense etre
                if(closestMonsterDistance > 3.1f)
                {
                    //Si notre destination etait le panneau de quete, c'est qu'on faisait demi tour
                    if (destination == questBoard.position) currentEtat = etat.Return;
                    //Sinon, si on avait une cible on retourne la trouver
                    else if (target != null) currentEtat = etat.Rush;
                    //Sinon, on marche
                    else currentEtat = etat.Walk;
                }
                break;

            case etat.Wait:
                //On decremente le timer et on se barre si on est a la fin
                waitTimer -= Time.fixedDeltaTime;
                askTimer -= Time.fixedDeltaTime;
                if (waitTimer <= 0) currentEtat = etat.Walk;
                //Sinon, si on peut faire une nouvelle demande d'ami on y va
                else if(askTimer <= 0)
                {
                    //On trouve un mec au pif dans le champs de vision (si il y en a) et on lui demande
                    if (humainsAutour.Count > 0)
                    {
                        copain = humainsAutour[Random.Range(0, humainsAutour.Count)].ReceiveCoopDemand(currentQuete, this);
                        //Si on s'est trouve un pote on y va !
                        if (copain != null)
                        {
                            Color couleur = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                            Tshirt.material.SetColor("_Color", couleur);
                            copain.GetNewTShirt(couleur);
                            copain.GetNewQuete(currentQuete);
                            currentEtat = etat.Walk;
                        }
                        //Sinon, on reset l'horloge
                        else askTimer = 1f;
                    }
                    //Si il y avait personne, l'horloge est peu reset
                    else askTimer = 0.1f;
                }
                break;
        }
    }

    //Les entites entrent dans le champ de vision, il faut les reconnaitre
    private void OnTriggerEnter(Collider other)
    {
        //Si l'entite est sur le layer de monstres, on l'ajoute a la liste
        if (mobLayer == (mobLayer | (1 << other.gameObject.layer)))
        {
            monstresAutour.Add(other.gameObject.GetComponentInParent<AMob>());
            //Si l'entite a le bon tag, on la definit comme cible et on va vers elle
            if (currentEtat == etat.Walk && !currentQuete.GetQuestDone() && other.gameObject.tag == currentQuete.GetTag())
            {
                target = other.gameObject.GetComponentInParent<Rigidbody>();
                if (copain != null) copain.GetNewTarget(target);
                currentEtat = etat.Rush;
            }
        }
        else if (humanLayer == (humanLayer | (1 << other.gameObject.layer))) humainsAutour.Add(other.GetComponentInParent<HumanScript>());
    }

    //Les entites qui sortent du champ de vision, il faut les oublier
    private void OnTriggerExit(Collider other)
    {
        //Si l'entite est sur le layer de monstres, on la retire de la liste
        if (mobLayer == (mobLayer | (1 << other.gameObject.layer))) monstresAutour.Remove(other.GetComponentInParent<AMob>());
        else if (humanLayer == (humanLayer | (1 << other.gameObject.layer))) humainsAutour.Remove(other.GetComponentInParent<HumanScript>());
    }

    //On s'assure bien que tous les monstres dans la memoire sont actifs
    private void VerifierMonstres()
    {
        for (int i = monstresAutour.Count - 1; i >= 0; i--)
        {
            if (!monstresAutour[i].gameObject.activeSelf) monstresAutour.RemoveAt(i);
        }
    }

    //Pour s'assurer qu'on voit pas des gens qui sont morts
    private void VerifierHumains()
    {
        for (int i = humainsAutour.Count - 1; i >= 0; i--)
        {
            //On retire les humains inactifs, et on desactive notre pote si il est mort
            if (!humainsAutour[i].gameObject.activeSelf)
            {
                if (humainsAutour[i] == copain) copain = null;
                humainsAutour.RemoveAt(i);
            }
        }
    }

    //Pour verifier qui est le monstre le plus proche, et si on doit le fuir
    private void GetClosestMonster()
    {
        closestMonsterDistance = 1000;
        closestMonster = null;
        foreach (AMob monstre in monstresAutour)
        {
            //On esquive les monstres qui ne font pas partis de la quete
            if (Vector3.Distance(monstre.GetComponentInParent<Rigidbody>().position, humanRigidbody.position) < closestMonsterDistance && monstre.tag != currentQuete.GetTag())
            {
                closestMonster = monstre.GetComponentInParent<Rigidbody>();
                closestMonsterDistance = Vector3.Distance(closestMonster.position, humanRigidbody.position);
            }
        }
    }

    //On est assez proche du panneau de quete, que se passe-t-il ?
    private void CloseToQuestBoard()
    {
        //Si la quete est finie, on gagne en exp (et peut en niveau) et on prend une nouvelle quete
        if (currentQuete.GetQuestDone())
        {
            experience += currentQuete.getRecompense();
            if (experience >= 100 * niveau) gagnerNiveau();

            //On perd notre pote, on gagne une quete, on se met a spam les invites
            copain = null;
            Tshirt.material.SetColor("_Color", Color.black);
            currentQuete = new Quete(niveau);
            waitTimer = currentQuete.getPeril() / Random.Range(2f, 5f);
            currentEtat = etat.Wait;
        }
        //Et dans tout les cas on se heal et on retourne au charbon
        currentPointsDeVie = pointsDeVie;
        //Si on a pas de pote, c'est le moment d'en chercher un
        if (copain == null)
        {
            waitTimer = currentQuete.getPeril() / Random.Range(2f, 5f);
            currentEtat = etat.Wait;
        }
        else currentEtat = etat.Walk;
    }

    public void OnDeath(Damageable attacker)
    {
        attacker.OnKill();
        gameObject.SetActive(false);
    }

    public void OnKill()
    {
        //On renseigne le tag de ce qu'on vient de tuer
        currentQuete.gotKilled(target.tag);
        //On retire le monstre des monstres autour
        monstresAutour.Remove(target.GetComponentInParent<AMob>());
        //On reset la cible
        target = null;
        //Si on a fini la quete, on revient au panneau
        if (currentQuete.GetQuestDone())
        {
            currentEtat = etat.Return;
            destination = questBoard.position;
        }
        //Sinon, on continue la quete
        else
        {
            float plusProche = 1000;
            //On cherche le bon mob le plus proche de nous
            foreach(AMob monstre in monstresAutour)
            {
                if(monstre.tag == currentQuete.GetTag() && Vector3.Distance(monstre.GetComponentInParent<Rigidbody>().position, humanRigidbody.position) < plusProche)
                {
                    target = monstre.GetComponentInParent<Rigidbody>();
                    plusProche = Vector3.Distance(target.position, humanRigidbody.position);
                }
            }
            //Dans le cas ou il n'y en a pas, on va voir ailleurs
            if(target == null) currentEtat = etat.Walk;
        }
        if (copain != null) copain.GetNewTarget(target);
    }

    public void TakeDamage(int damages, Damageable attacker)
    {
        currentPointsDeVie -= damages;
        if (currentPointsDeVie <= 0) OnDeath(attacker);
        else if (damages * Random.Range(1f, monstresAutour.Count) >= currentPointsDeVie)
        {
            currentEtat = etat.Return;
            destination = questBoard.position;
        }
    }

    public Rigidbody GetRigidbody()
    {
        return gameObject.GetComponent<Rigidbody>();
    }
}
