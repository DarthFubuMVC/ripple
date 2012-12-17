cd \code\ripple
bundle exec rake
bundle exec rake publish
cd \code\buildsupport
git commit -a -m "new stuff"
git push origin
cd \code\fubucore\buildsupport
git pull && cd \code\fubucore

git commit -a -m "new stuff"
git push origin
