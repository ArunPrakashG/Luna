#!/bin/bash
# raspberry pi initial setup script
# TODO

sudo apt-get install festival
TEMP_DEB="$(mktemp)" &&
wget -O "$TEMP_DEB" 'http://ftp.cn.debian.org/debian/pool/main/f/festvox-us-slt-hts/festvox-us-slt-hts_0.2010.10.25-3_all.deb' &&
sudo dpkg -i "$TEMP_DEB"
rm -f "$TEMP_DEB"

echo "(set! voice_default 'voice_cmu_us_slt_arctic_hts)" | sudo tee -a /etc/festival.scm