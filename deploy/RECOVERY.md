# CustomerReturnCRM Disaster Recovery

GitHub contains the application, Dockerfiles, Compose configuration, EF migrations, CI/CD workflow, and VPS/Nginx bootstrap files.

## Keep outside GitHub

Never commit production secrets. Keep a secure backup of:
- `/opt/CustomerReturnCRM/.env`
- The GitHub Actions SSH private key
- The production deploy user's GitHub SSH private key
- Other production-only credentials

## Fresh VPS

1. Install Ubuntu 24.04 and point `bemooni.ir` and `www.bemooni.ir` to the new VPS.
2. As root:

   ```bash
   git clone https://github.com/mohamadrahmani/CustomerReturnCRM.git /opt/CustomerReturnCRM
   cd /opt/CustomerReturnCRM
   bash deploy/server-setup.sh
   ```

3. Restore the production `.env`:

   ```bash
   cp /secure-backup/.env /opt/CustomerReturnCRM/.env
   chown deploy:deploy /opt/CustomerReturnCRM/.env
   chmod 600 /opt/CustomerReturnCRM/.env
   ```

4. Configure GitHub access for `deploy`. Generate a new key, or restore the existing deploy private key, then add the public key as a read-only GitHub Deploy Key.

   ```bash
   su - deploy
   ssh-keygen -t ed25519 -C "customerreturncrm-deploy-github" -f ~/.ssh/id_ed25519
   cat ~/.ssh/id_ed25519.pub
   ```

   Add that public key in GitHub: **Repository → Settings → Deploy keys**.

5. Switch the repository remote to SSH and verify:

   ```bash
   cd /opt/CustomerReturnCRM
   git remote set-url origin git@github.com:mohamadrahmani/CustomerReturnCRM.git
   ssh -T git@github.com
   git pull --ff-only origin main
   ```

6. Validate production configuration:

   ```bash
   docker compose config --quiet
   ```

7. Run migrations:

   ```bash
   docker compose run --rm migration
   ```

8. Start the application:

   ```bash
   docker compose up -d api web
   ```

9. Configure HTTPS after DNS is working:

   ```bash
   certbot --nginx -d bemooni.ir -d www.bemooni.ir
   ```

10. Restore/configure the GitHub Actions VPS SSH key and GitHub Secrets: `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY`.

## Database

The production SQL Server is external to this VPS. Reinstalling the VPS does not restore the database. Maintain independent SQL Server full backups, retention, independent storage, and periodic restore tests. EF migrations are not database backups.

## Recovery test

Test this procedure on a temporary VPS before relying on it in an emergency:
- fresh Ubuntu
- bootstrap
- restore `.env`
- configure GitHub SSH
- run migrations
- start containers
- issue SSL certificate
- verify login and core API operations
- verify GitHub Actions deployment
