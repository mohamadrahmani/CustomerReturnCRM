# CustomerReturnCRM Disaster Recovery

GitHub contains the application, Dockerfiles, Compose configuration, EF migrations, CI/CD workflow, and VPS/Nginx bootstrap files.

## Keep outside GitHub
- /opt/CustomerReturnCRM/.env
- GitHub Actions SSH private key
- Production deploy user's GitHub SSH private key
- Other production-only credentials

## Fresh VPS
1. Install Ubuntu 24.04 and point bemooni.ir and www.bemooni.ir to the new VPS.
2. As root:
   git clone https://github.com/mohamadrahmani/CustomerReturnCRM.git /opt/CustomerReturnCRM
   cd /opt/CustomerReturnCRM
   bash deploy/server-setup.sh
3. Restore .env, then:
   chown deploy:deploy /opt/CustomerReturnCRM/.env
   chmod 600 /opt/CustomerReturnCRM/.env
4. Create a new deploy SSH key and add its public key as a read-only GitHub Deploy Key.
5. Verify deploy can run git pull --ff-only origin main.
6. Run: docker compose config --quiet
7. Run: docker compose run --rm migration
8. Run: docker compose up -d api web
9. Run: certbot --nginx -d bemooni.ir -d www.bemooni.ir
10. Restore/configure GitHub Actions SSH and the VPS_HOST, VPS_USER, VPS_SSH_KEY GitHub Secrets.

## Database
The production SQL Server is external to this VPS. VPS reinstall does not restore the database. Maintain independent SQL Server full backups, retention, independent storage, and periodic restore tests. EF migrations are not database backups.
