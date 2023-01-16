#import "UnityAppController.h"
#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

extern UIViewController *UnityGetGLViewController();

@interface iOSPlugin : NSObject

@end

@implementation iOSPlugin

@end

char* cStringCopy(const char* string)
{
    if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    
    return res;
}

extern "C"
{
    UIViewController * _GetAppViewController() {
        UIViewController * ret = UnityGetGLViewController();
        return ret;
    }
}
