package com.rokid.ar.creature;

import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import android.view.GestureDetector;
import android.view.MotionEvent;
import android.view.View;
import android.view.animation.RotateAnimation;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.Timer;
import java.util.TimerTask;

public class CreatureDisplayActivity extends AppCompatActivity {
    
    private static final String TAG = "CreatureDisplay";
    
    private ImageView creatureImageView;
    private TextView creatureNameText;
    private TextView creatureInfoText;
    private View creatureContainer;
    
    private float rotationAngle = 0f;
    private final float rotationSpeed = 1f;
    private Timer rotationTimer;
    
    private GestureDetector gestureDetector;
    
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_creature_display);
        
        initializeViews();
        setupGestureDetector();
        
        // Handle incoming creature data
        String creatureData = getIntent().getStringExtra("creature_data");
        if (creatureData != null) {
            displayCreature(creatureData);
        } else {
            showPlaceholder();
        }
    }
    
    private void initializeViews() {
        creatureImageView = findViewById(R.id.creature_image);
        creatureNameText = findViewById(R.id.creature_name);
        creatureInfoText = findViewById(R.id.creature_info);
        creatureContainer = findViewById(R.id.creature_container);
    }
    
    private void setupGestureDetector() {
        gestureDetector = new GestureDetector(this, new GestureListener());
        
        creatureContainer.setOnTouchListener((v, event) -> {
            gestureDetector.onTouchEvent(event);
            return true;
        });
    }
    
    private void displayCreature(String jsonData) {
        try {
            JSONObject creature = new JSONObject(jsonData);
            String creatureId = creature.getString("creatureId");
            String creatureName = creature.getString("creatureName");
            String modelPath = creature.getString("modelPath");
            
            // Set creature name
            creatureNameText.setText(creatureName);
            creatureInfoText.setText("ID: " + creatureId);
            
            // Load appropriate image based on creature type
            int drawableId = getCreatureDrawableId(creatureId);
            creatureImageView.setImageResource(drawableId);
            
            // Start 360-degree rotation
            start360Rotation();
            
            // Show success message
            Toast.makeText(this, "精灵已捕获！", Toast.LENGTH_SHORT).show();
            
        } catch (JSONException e) {
            Log.e(TAG, "Error parsing creature data", e);
            showPlaceholder();
        }
    }
    
    private int getCreatureDrawableId(String creatureId) {
        switch (creatureId.toLowerCase()) {
            case "dragon":
                return R.drawable.dragon_360;
            case "pikachu":
                return R.drawable.pikachu_360;
            case "cat":
                return R.drawable.cat_360;
            case "wolf":
                return R.drawable.wolf_360;
            default:
                return R.drawable.default_creature_360;
        }
    }
    
    private void showPlaceholder() {
        creatureNameText.setText("等待精灵...");
        creatureInfoText.setText("请通过Rokid眼镜捕获精灵");
        creatureImageView.setImageResource(R.drawable.placeholder_creature);
    }
    
    private void start360Rotation() {
        if (rotationTimer != null) {
            rotationTimer.cancel();
        }
        
        rotationTimer = new Timer();
        rotationTimer.scheduleAtFixedRate(new TimerTask() {
            @Override
            public void run() {
                runOnUiThread(() -> {
                    rotationAngle += rotationSpeed;
                    if (rotationAngle >= 360) {
                        rotationAngle = 0;
                    }
                    
                    creatureImageView.setRotation(rotationAngle);
                });
            }
        }, 0, 50); // Update every 50ms for smooth rotation
    }
    
    private class GestureListener extends GestureDetector.SimpleOnGestureListener {
        @Override
        public boolean onDoubleTap(MotionEvent e) {
            // Double tap to zoom
            float currentScale = creatureImageView.getScaleX();
            float newScale = currentScale == 1.0f ? 1.5f : 1.0f;
            creatureImageView.setScaleX(newScale);
            creatureImageView.setScaleY(newScale);
            return true;
        }
        
        @Override
        public boolean onScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY) {
            // Manual rotation via scroll
            rotationAngle -= distanceX * 0.5f;
            if (rotationAngle < 0) rotationAngle += 360;
            if (rotationAngle >= 360) rotationAngle -= 360;
            
            creatureImageView.setRotation(rotationAngle);
            return true;
        }
    }
    
    @Override
    protected void onDestroy() {
        super.onDestroy();
        if (rotationTimer != null) {
            rotationTimer.cancel();
            rotationTimer = null;
        }
    }
}