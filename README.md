# 🦊 SnoutBot - Outils et virtual banking

Snout est un ensemble de fonctionnalités utilitaires et de divertissement destinés à un usage local, sur des serveurs à petite population. Conçu grâce .NET 7.0, framework réputé bien plus rapide que ses prédecesseurs, et sur la base du nouveau système de slash-commands introduit par l'API Discord, il est intégralement asynchrone dans son exécution. Sans exploiter de serveur SQL, il est léger et fait usage d'un fichier de base de données SQLite.

Divisé en modules interactifs et désactivables, il s'étend à chaque mise à jour.

Il est initialement développé afin de répondre à un besoin de web-fetching vers le site Battlemetrics.com et ainsi faire remonter le statut des serveurs francophones du jeu Hell Let Loose™.

A ce jour, le module de banking est le plus important et vise à représenter à simuler un système bancaire simplifié. Les utilisateurs gagnent des € virtuels sur la base de rémunération de l'activité qu'ils effectuent sur les serveurs Discord que Snout surveille.

## ℹ️ Commandes

🛠️ **Commandes généralistes**
- **/ping** : renvoie le ping de la gateway API Discord
- **/register** : inscrit un utilisateur dans la base de données de Snout, utilisée dans les modules
- **/unregister** : retire un utilisateur de la base de données de Snout *(admin)*

🌍 **Commande du traducteur**
- **/t** : traduire un texte vers l'une des langues supportées par DeepL™
- **/thelp** : connaître les langues cibles et le quota mensuel autorisé

💶 **Commandes du module de paycheck**
- **/mpaycheck** : active/désactive le système de rémunération basé sur des évènements Discord *(gère aussi la mise à jour quotidienne des comptes - admin)*

🏦 **Commandes du module de banking**
- **/newaccount** : créer un nouveau compte bancaire courant ou d'épargne et l'assigne à un utilisateur (l'utilisateur doit avoir utilisé */register*)
- **/editaccount** : éditer les paramètres d'un compte bancaire, tels que la limite de découvert, les frais de service ou le taux d'intérêt *(admin)*
- **/myaccounts** : afficher le statut de ses comptes bancaires. *(résultats en messages privés)*
- **/checkaccounts** : vérifier le statut des comptes bancaires d'un utilisateur. *(résultats en messages privés - admin)*
- **/deposit** : ajouter de l'argent à un compte bancaire *(admin)*
- **/withdraw** : retirer de l'argent d'un compte bancaire
- **/transfer** : faire un virement entre deux comptes

🪖 **Commandes du module web-fetcher** *(Hell Let Loose™ uniquement, via Battlemetrics.com)*
- **/mfetcher** : active/désactive l'auto-fetcher dans le canal ciblé par la commande. Il est recommandé de dédier un canal pour cette action, sur votre serveur. *(admin)*
- **/add** : permet d'ajouter un nouveau serveur au fetcher par utilisation de son URL battlemetrics


## 🔑 Authentification & Droits

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `Tokens/[token.txt]` | `file w/ string` | **Requis**. Token de bot Discord  |
| `Tokens/[deepl.txt]` | `file w/ string` | **Requis**. Clef d'API DeepL (gratuite, à récupérer sur https://www.deepl.com/fr/pro-api?cta=header-pro-api/)  |

⚠ **Commandes admin-only à définir dans les paramètres du serveur Discord** ⚠️:
```
/unregister
/editaccount
/checkaccounts
/deposit
/mfetcher
/mpaycheck
```

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

- **1.3** : Bientôt

## 🦊 Développement

- [@RedTheFoxx](https://github.com/RedTheFoxx)
