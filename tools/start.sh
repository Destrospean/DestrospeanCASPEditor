#!/bin/bash
cd "${0%/*}"
mkdir registry
MONO_REGISTRY_PATH=`pwd`/registry LD_LIBRARY_PATH=.:$LD_LIBRARY_PATH ./CASDesignerToolkit
