# 🦊 SnoutBot - Outils et virtual banking

Snout est un ensemble de fonctionnalités utilitaires et de divertissement destinés à un usage local, sur des serveurs à petite population. Conçu grâce .NET 7.0, framework réputé bien plus rapide que ses prédecesseurs, et sur la base du nouveau système de slash-commands introduit par l'API Discord, il est intégralement asynchrone dans son exécution. Sans exploiter de serveur SQL, il est léger et fait usage d'un fichier de base de données SQLite.

Divisé en modules interactifs et désactivables, il s'étend à chaque mise à jour.

A ce jour, le module de banking est le plus important et vise à représenter à simuler un système bancaire simplifié. Les utilisateurs gagnent des € virtuels sur la base de rémunération de l'activité qu'ils effectuent sur les serveurs Discord que Snout surveille.

Un compte courant *("checkings")* est __unique__ et reçoit les paiements issus de l'activité Discord. C'est un compte à intérêts limités. 

Un compte d'épargne *("savings")* ne sert qu'au dépôt afin de sécuriser ses gains. Un utilisateur peut en avoir une infinité. Il est recommandé d'associer des taux d'intérêts plus forts sur ces derniers, tout en limitant les frais de service.

## ℹ️ Commandes
_(Commands in english are considered higher risk than the others and must be monitored by admins)_

💶 **Administration des plugins**
- **/module paycheck** : active/désactive le système de rémunération basé sur des évènements Discord *(gère aussi la mise à jour quotidienne des comptes)*
- **/module fetcher** : active/désactive l'auto-fetcher dans le canal ciblé par la commande. Il est recommandé de dédier un canal pour cette action, sur votre serveur.

🛠️ **Gestion des utilisateurs**
- **/utilisateurs ajouter** : inscrit un utilisateur dans la base de données de Snout, utilisée dans les modules
- **/utilisateurs delete** : retire un utilisateur de la base de données de Snout

🏦 **Gestion bancaire**
- **/banque nouveau** : créer un nouveau compte bancaire courant ou d'épargne et l'assigne à un utilisateur (l'utilisateur doit être enregistré dans Snout)
- **/banque edit** : éditer les paramètres d'un compte bancaire, tels que la limite de découvert, les frais de service ou le taux d'intérêt
- **/banque mescomptes** : afficher le statut de ses comptes bancaires. *(résultats en messages privés)*
- **/banque check** : vérifier le statut des comptes bancaires d'un utilisateur. *(résultats en messages privés)*
- **/banque deposit** : ajouter de l'argent à un compte bancaire
- **/banque retirer** : retirer de l'argent d'un compte bancaire
- **/banque virement** : faire un virement entre deux comptes

🌍 **Service de traduction**
- **/t traduire** : traduire un texte vers l'une des langues supportées par DeepL™
- **/t aide** : connaître les langues cibles et le quota mensuel autorisé

🪖 **Commandes du module web-fetcher** *(Hell Let Loose™ uniquement, via Battlemetrics.com)*
- **/url ajouter** : permet d'ajouter un nouveau serveur au fetcher par utilisation de son URL battlemetrics

🛠️ **Généraliste**
- **/ping** : renvoie le ping de la gateway API Discord

## 🔑 Authentification & Droits

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `Tokens/[token.txt]` | `file w/ string` | **Requis**. Token de bot Discord  |
| `Tokens/[deepl.txt]` | `file w/ string` | **Requis**. Clef d'API DeepL (gratuite, à récupérer sur https://www.deepl.com/fr/pro-api?cta=header-pro-api/)  |

## 🗂️ Déploiement

Snout requiert l'utilisation d'une base de données type SQLITE (*version 3*) dont le fichier de génération doit être placé dans le
dossier. Elle sera ensuite générée automatiquement :
```bash
  ./SQL/[GenerateDB.sql]
```
Sauvegardez vos données avant de mettre à jour le bot car la base de données peut évoluer en structure.

Le runtime .NET 7.0 doit être installé sur la machine hôte.

Une fois compilé, le bot est exécuté comme un programme Win64 :
```bash
  ./snout.exe
  
  OU
  
  dotnet snout.dll 
```

## 🚧 Roadmap

- **1.3** : _TBA_

## 🦊 Développement

- [@RedTheFoxx](https://github.com/RedTheFoxx)
