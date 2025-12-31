#!/bin/bash
cd "${0%/*}"
LD_LIBRARY_PATH=.:$LD_LIBRARY_PATH ./DestrospeanCASPEditor
