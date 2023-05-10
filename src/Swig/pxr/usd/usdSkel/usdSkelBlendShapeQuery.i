%module UsdSkelBlendShapeQuery

%{
#include "pxr/usd/usdSkel/blendShapeQuery.h"
#include "pxr/base/vt/array.h"
%}

%template(VtIntArrayVector) std::vector<VtIntArray>;

%include "pxr/usd/usdSkel/blendShapeQuery.h"
